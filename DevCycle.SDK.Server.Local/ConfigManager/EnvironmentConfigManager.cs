using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DevCycle.SDK.Server.Local.Api;
using DevCycle.SDK.Server.Common.API;
using DevCycle.SDK.Server.Common.Exception;
using DevCycle.SDK.Server.Common.Model;
using DevCycle.SDK.Server.Common.Model.Local;
using DevCycle.SDK.Server.Common.Policies;
using LaunchDarkly.EventSource;
using Microsoft.Extensions.Logging;
using RestSharp;
using ErrorResponse = DevCycle.SDK.Server.Common.Model.ErrorResponse;

namespace DevCycle.SDK.Server.Local.ConfigManager
{
    public class EnvironmentConfigManager : IDisposable
    {
        private const int MinimumPollingIntervalMs = 1000;
        private const int SsePollingIntervalMs = 15 * 60 * 1000;
        private const int SseErrorRestartThreshold = 5;

        private readonly string sdkKey;
        private readonly int pollingIntervalMs;
        private readonly int requestTimeoutMs;
        private readonly RestClient restClient;
        private readonly ILogger logger;
        private readonly DevCycleEventArgs initializationEvent;
        private readonly LocalBucketing localBucketing;
        private readonly EventHandler<DevCycleEventArgs> initializedHandler;
        private readonly DevCycleLocalOptions localOptions;
        private EventQueue eventQueue;
        private Timer pollingTimer;

        private bool pollingEnabled = true;
        private SSEManager sseManager;
        private string configEtag = "";
        private string configLastModified = "";
        // REPLACED fetchInProgress int with SemaphoreSlim for proper async mutual exclusion
        private readonly SemaphoreSlim fetchLock = new(1, 1);
        private DateTime lastFetchTime = DateTime.MinValue;
        private int consecutiveSseErrors = 0;
        private DateTime lastSseErrorTime = DateTime.MinValue;

        public virtual string Config { get; private set; }
        public virtual bool Initialized { get; internal set; }
        internal int CurrentPollingIntervalMs { get; private set; }
        internal int ConsecutiveSseErrorCount => consecutiveSseErrors;
        internal void TestInvokeSseError(Exception ex = null) => SSEErrorHandler(this, new ExceptionEventArgs(ex ?? new Exception("test")));
        internal void TestInvokeSseState(ReadyState state) => SSEStateHandler(this, new StateChangedEventArgs(state));
        internal void SetSseManager(SSEManager manager) => sseManager = manager;

        public EnvironmentConfigManager(
            string sdkKey,
            DevCycleLocalOptions dvcLocalOptions,
            ILoggerFactory loggerFactory,
            LocalBucketing localBucketing,
            EventHandler<DevCycleEventArgs> initializedHandler = null,
            DevCycleRestClientOptions restClientOptions = null
        )
        {
            localOptions = dvcLocalOptions;
            this.sdkKey = sdkKey;

            pollingIntervalMs = dvcLocalOptions.ConfigPollingIntervalMs >= MinimumPollingIntervalMs
                ? dvcLocalOptions.ConfigPollingIntervalMs
                : MinimumPollingIntervalMs;
            requestTimeoutMs = dvcLocalOptions.ConfigPollingTimeoutMs <= pollingIntervalMs
                ? pollingIntervalMs
                : dvcLocalOptions.ConfigPollingTimeoutMs;
            dvcLocalOptions.CdnCustomHeaders ??= new Dictionary<string, string>();

            DevCycleRestClientOptions clientOptions = restClientOptions?.Clone() ?? new DevCycleRestClientOptions();
            clientOptions.BaseUrl = new Uri(dvcLocalOptions.CdnUri);

            restClient = new RestClient(clientOptions);
            restClient.AddDefaultHeaders(dvcLocalOptions.CdnCustomHeaders);

            logger = loggerFactory.CreateLogger<EnvironmentConfigManager>();
            this.localBucketing = localBucketing;
            initializationEvent = new DevCycleEventArgs();

            if (initializedHandler != null)
            {
                this.initializedHandler += initializedHandler;
            }
        }

        internal void SetEventQueue(EventQueue queue)
        {
            eventQueue = queue;
        }

        public async Task InitializeConfigAsync()
        {
            try
            {
                await FetchConfigAsyncWithTask().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to download config");
                throw;
            }
            finally
            {
                OnInitialized(initializationEvent);
                if (pollingEnabled)
                {
                    EnsurePollingTimer(pollingIntervalMs, pollingIntervalMs);
                }
            }
        }

        public void Dispose()
        {
            StopPolling();
            sseManager?.CloseSSE();
            (sseManager as IDisposable)?.Dispose();
            restClient.Dispose();
            fetchLock?.Dispose();
        }

        private void OnInitialized(DevCycleEventArgs e)
        {
            Initialized = e.Success;
            initializedHandler?.Invoke(this, e);
        }

        private string GetConfigUrl()
        {
            return localOptions.CdnSlug != "" ? localOptions.CdnSlug : $"/config/v2/server/{sdkKey}.json";
        }

        private bool ShouldFetchBasedOnSseLastModified(uint sseLastModified)
        {
            if (sseLastModified == 0) return true;
            if (string.IsNullOrEmpty(configLastModified)) return true;
            try
            {
                var stored = Convert.ToDateTime(configLastModified);
                var storedUnix = new DateTimeOffset(stored).ToUnixTimeSeconds();
                return sseLastModified > (uint)storedUnix;
            }
            catch
            {
                return true;
            }
        }

        private async Task FetchConfigAsyncWithTask(uint lastmodified = 0)
        {
            if (!pollingEnabled)
            {
                return;
            }
            // Attempt to enter lock without waiting; skip if a fetch is already running
            if (!await fetchLock.WaitAsync(0).ConfigureAwait(false))
            {
                return;
            }

            try
            {
                if (lastmodified != 0 && !ShouldFetchBasedOnSseLastModified(lastmodified))
                {
                    logger.LogDebug("Skipping config fetch; SSE lastModified ({LastModified}) not newer than stored header", lastmodified);
                    return;
                }

                var cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromMilliseconds(requestTimeoutMs));
                var request = new RestRequest(GetConfigUrl());
                if (!string.IsNullOrEmpty(configEtag)) request.AddHeader("If-None-Match", configEtag);
                if (!string.IsNullOrEmpty(configLastModified)) request.AddHeader("If-Modified-Since", configLastModified);

                RestResponse res = await ClientPolicy.GetInstance().RetryOncePolicy
                    .ExecuteAsync(() => restClient.ExecuteAsync(request, cts.Token));
                lastFetchTime = DateTime.UtcNow;
                initializationEvent.Success = true;
                DevCycleException finalError;

                switch (res.StatusCode)
                {
                    case >= HttpStatusCode.InternalServerError or 0:
                        if (Config != null)
                        {
                            logger.LogError(res.ErrorException,
                                "Failed to download config, using cached version: {ConfigEtag}, {Lastmodified}", configEtag,
                                configLastModified);
                        }
                        else
                        {
                            initializationEvent.Success = false;
                            logger.LogError(res.ErrorException, "Failed to download initial DevCycle config");
                        }
                        break;
                    case >= HttpStatusCode.BadRequest:
                    {
                        initializationEvent.Success = false;
                        var errorMessage = (int)res.StatusCode == 403
                            ? "Project configuration could not be found. Check your SDK key."
                            : "Encountered non-retryable error fetching config. Client will not attempt to fetch configuration again.";
                        finalError = new DevCycleException(res.StatusCode,
                            new ErrorResponse(errorMessage));
                        StopPolling();
                        logger.LogError(finalError.ErrorResponse.Message);
                        initializationEvent.Errors.Add(finalError);
                        break;
                    }
                    case HttpStatusCode.NotModified:
                        logger.LogDebug(
                            "Config not modified, using cache, etag: {ConfigEtag}, lastmodified: {lastmodified}",
                            configEtag, configLastModified);
                        break;
                    default:
                        try
                        {
                            var lastModified = res.ContentHeaders?.FirstOrDefault(e => e.Name?.ToLower() == "last-modified")?.Value as string;
                            var etag = res.Headers?.FirstOrDefault(e => e.Name?.ToLower() == "etag")?.Value as string;
                            if (!string.IsNullOrEmpty(configLastModified) && lastModified != null &&
                                !string.IsNullOrEmpty(lastModified))
                            {
                                try
                                {
                                    var parsedHeader = Convert.ToDateTime(lastModified);
                                    var storedHeader = Convert.ToDateTime(configLastModified);
                                    if (DateTime.Compare(storedHeader, parsedHeader) >= 0)
                                    {
                                        logger.LogWarning("Received timestamp on last-modified that was before the stored one. Not updating config.");
                                        return;
                                    }
                                }
                                catch (Exception timeParseEx)
                                {
                                    logger.LogWarning(timeParseEx, "Failed to parse last-modified headers; proceeding with update");
                                }
                            }

                            try
                            {
                                var minimalConfig = JsonDocument.Parse(res.Content);
                                var sseProp = minimalConfig.RootElement.GetProperty("sse");
                                var hostname = sseProp.GetProperty("hostname").GetString();
                                var path = sseProp.GetProperty("path").GetString();
                                var sseUri = (hostname ?? "") + (path ?? "");
                                if (!string.IsNullOrEmpty(sseUri) && !localOptions.DisableRealtimeUpdates)
                                {
                                    if (!sseUri.StartsWith("http"))
                                    {
                                        sseUri = "https://" + sseUri.TrimStart('/');
                                    }
                                    if (sseManager == null)
                                    {
                                        sseManager = new SSEManager(sseUri, SSEStateHandler, SSEMessageHandler, SSEErrorHandler);
                                        sseManager.StartSSE();
                                    }
                                    else
                                    {
                                        sseManager.RestartSSE(sseUri);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                logger.LogWarning(e, "Failed to parse SSE config. Skipping SSE Initialization");
                            }

                            localBucketing.StoreConfig(sdkKey, res.Content);
                            configEtag = etag;
                            configLastModified = lastModified;
                            Config = res.Content;
                            logger.LogDebug("Config successfully initialized with etag: {ConfigEtag}, {lastmodified}", configEtag, configLastModified);
                            Initialized = true;
                        }
                        catch (Exception e)
                        {
                            logger.LogError(e, "Failed to set config: {Message} {InnerMessage}", e.Message, e.InnerException?.Message);
                        }

                        break;
                }
            }
            finally
            {
                fetchLock.Release();
            }
        }

        private async void SSEMessageHandler(object sender, MessageReceivedEventArgs args)
        {
            try
            {
                var message = JsonSerializer.Deserialize<SSEMessage>(args.Message.Data);
                if (message?.Type is "refetchConfig" or "")
                {
                    await FetchConfigAsyncWithTask(message.LastModified);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to process SSE message");
            }
        }

        private void SSEErrorHandler(object sender, ExceptionEventArgs args)
        {
            consecutiveSseErrors++;
            lastSseErrorTime = DateTime.UtcNow;
            logger.LogWarning(args.Exception, "SSE Connection Returned an error (count={Count})", consecutiveSseErrors);
            if (pollingEnabled)
            {
                EnsurePollingTimer(pollingIntervalMs, pollingIntervalMs);
                if (DateTime.UtcNow - lastFetchTime > TimeSpan.FromMilliseconds(pollingIntervalMs))
                {
                    _ = FetchConfigAsyncWithTask();
                }
            }
            if (consecutiveSseErrors >= SseErrorRestartThreshold && sseManager != null)
            {
                logger.LogInformation("SSE error threshold reached; forcing restart");
                consecutiveSseErrors = 0;
                sseManager.RestartSSE();
            }
        }

        private void SSEStateHandler(object sender, StateChangedEventArgs args)
        {
            switch (args.ReadyState)
            {
                case ReadyState.Raw:
                    break;
                case ReadyState.Connecting:
                    break;
                case ReadyState.Open:
                    consecutiveSseErrors = 0;
                    EnsurePollingTimer(SsePollingIntervalMs, SsePollingIntervalMs);
                    logger.LogInformation("Connected to SSE - setting polling to 15 minutes");
                    break;
                case ReadyState.Closed:
                case ReadyState.Shutdown:
                    logger.LogInformation("SSE Shutdown - reverting polling interval to base");
                    EnsurePollingTimer(pollingIntervalMs, pollingIntervalMs);
                    break;
            }
        }

        private async void FetchConfigAsync(object state = null)
        {
            try
            {
                await FetchConfigAsyncWithTask();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Unexpected error during config polling");
            }
        }

        private void EnsurePollingTimer(int dueMs, int periodMs)
        {
            if (!pollingEnabled) return;
            CurrentPollingIntervalMs = periodMs;
            if (pollingTimer == null)
            {
                pollingTimer = new Timer(FetchConfigAsync, null, dueMs, periodMs);
            }
            else
            {
                try
                {
                    pollingTimer.Change(dueMs, periodMs);
                }
                catch (ObjectDisposedException)
                {
                    pollingTimer = new Timer(FetchConfigAsync, null, dueMs, periodMs);
                }
            }
        }

        private void StopPolling()
        {
            pollingTimer?.Dispose();
            pollingEnabled = false;
        }
    }
}