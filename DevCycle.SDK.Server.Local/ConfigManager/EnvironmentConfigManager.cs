using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DevCycle.SDK.Server.Local.Api;
using DevCycle.SDK.Server.Common.API;
using DevCycle.SDK.Server.Common.Exception;
using DevCycle.SDK.Server.Common.Model;
using DevCycle.SDK.Server.Common.Model.Local;
using DevCycle.SDK.Server.Common.Policies;
using Microsoft.Extensions.Logging;
using RestSharp;
using ErrorResponse = DevCycle.SDK.Server.Common.Model.ErrorResponse;
using Wasmtime;

namespace DevCycle.SDK.Server.Local.ConfigManager
{
    public class EnvironmentConfigManager : IDisposable
    {
        private const int MinimumPollingIntervalMs = 1000;

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

        public virtual string Config { get; private set; }
        public virtual bool Initialized { get; internal set; }

        private bool PollingEnabled = true;

        private string configEtag = "";
        private string configLastModified = "";

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
            // Explicitly override the base URL to use the one in local bucketing options. This allows the normal 
            // rest client options to override the Events api endpoint url; while sharing certificate and other information.
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
                // check if polling is still enabled, we might have hit a non-retryable error
                if (PollingEnabled)
                {
                    pollingTimer = new Timer(FetchConfigAsync, null, pollingIntervalMs, pollingIntervalMs);
                }
            }
        }

        public void Dispose()
        {
            StopPolling();
            restClient.Dispose();
        }

        private void OnInitialized(DevCycleEventArgs e)
        {
            Initialized = true;
            initializedHandler?.Invoke(this, e);
        }

        private string GetConfigUrl()
        {
            return localOptions.CdnSlug != "" ? localOptions.CdnSlug : $"/config/v2/server/{sdkKey}.json";
        }

        /**
         * Fetches the config from the server and stores it in the local bucketing manager.
         * This method should never throw on recoverable server errors. A 5xx error or other problem will only be
         * logged. Any error caused by the user (e.g. a 400 error) will be communicated via the initializationEvent
         * which is passed to the registered callback on the client.
         *
         * Unexpected exceptions will still be thrown from here.
         */
        private async Task FetchConfigAsyncWithTask()
        {
            if (!PollingEnabled)
            {
                return;
            }

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMilliseconds(requestTimeoutMs));
            var request = new RestRequest(GetConfigUrl());
            if (configEtag != null) request.AddHeader("If-None-Match", configEtag);
            if (configLastModified != null) request.AddHeader("If-Modified-Since", configLastModified);

            RestResponse res = await ClientPolicy.GetInstance().RetryOncePolicy
                .ExecuteAsync(() => restClient.ExecuteAsync(request, cts.Token));
            // initialization is always a success unless a user-caused error occurs (ie. a 4xx error)
            initializationEvent.Success = true;
            DevCycleException finalError;
            
            // Status code of 0 means some other error (like a network error) occurred
            if (res.StatusCode >= HttpStatusCode.InternalServerError || res.StatusCode == 0)
            {
                if (Config != null)
                {
                    logger.LogError(res.ErrorException, "Failed to download config, using cached version: {ConfigEtag}, {Lastmodified}", configEtag, configLastModified);
                }
                else
                {
                    logger.LogError(res.ErrorException,"Failed to download DevCycle config");
                }
            }
            else if (res.StatusCode >= HttpStatusCode.BadRequest)
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
            }
            else if (res.StatusCode == HttpStatusCode.NotModified)
            {
                logger.LogDebug("Config not modified, using cache, etag: {ConfigEtag}, lastmodified: {lastmodified}", configEtag, configLastModified);
            }
            else
            {
                try
                {
                    var lastModified = res.ContentHeaders?.FirstOrDefault(e => e.Name?.ToLower() == "last-modified")?.Value as string;
                    var etag = res.Headers?.FirstOrDefault(e => e.Name?.ToLower() == "etag")?.Value as string;
                    if (!string.IsNullOrEmpty(configLastModified) && lastModified != null && !string.IsNullOrEmpty(lastModified) )
                    {
                        var parsedHeader = Convert.ToDateTime(lastModified);
                        var storedHeader = Convert.ToDateTime(configLastModified);
                        // negative means that the stored header is before the returned parsed header
                        if (DateTime.Compare(storedHeader, parsedHeader) >= 0)
                        {
                            logger.LogWarning("Received timestamp on last-modified that was before the stored one. Not updating config.");
                            return;
                        }
                    }
                    
                    localBucketing.StoreConfig(sdkKey, res.Content);
                    configEtag = etag;
                    configLastModified = lastModified;
                    logger.LogDebug("Config successfully initialized with etag: {ConfigEtag}, {lastmodified}", configEtag, configLastModified);
                    //eventQueue?.QueueSDKConfigEvent(request, res);
                }
                catch (Exception e)
                {
                    // This is to catch any exception that is thrown by the SetConfig method if the config is not valid
                    logger.LogError($"Failed to set config: {e.Message} {e.InnerException.Message}");
                }
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

        private void StopPolling()
        {
            pollingTimer?.Dispose();
            PollingEnabled = false;
        }
    }
}