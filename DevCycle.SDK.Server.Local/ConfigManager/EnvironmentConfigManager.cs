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
using Microsoft.Extensions.Logging.Abstractions;
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
        private readonly ILocalBucketing localBucketing;
        private readonly DVCEventArgs dvcEventArgs;
        private readonly EventHandler<DVCEventArgs> initializedHandler;
        private readonly DVCLocalOptions localOptions;
        private Timer pollingTimer;

        public virtual string Config { get; private set; }
        public virtual bool Initialized { get; internal set; }

        private bool PollingEnabled = true;

        private string configEtag;
        private bool alreadyCalledHandler;

        public EnvironmentConfigManager(
            string sdkKey, 
            DVCLocalOptions dvcLocalOptions,
            ILoggerFactory loggerFactory,
            ILocalBucketing localBucketing,
            EventHandler<DVCEventArgs> initializedHandler = null,
            DVCRestClientOptions restClientOptions = null
        ) {
            localOptions = dvcLocalOptions;
            this.sdkKey = sdkKey;

            pollingIntervalMs = dvcLocalOptions.ConfigPollingIntervalMs >= MinimumPollingIntervalMs
                ? dvcLocalOptions.ConfigPollingIntervalMs
                : MinimumPollingIntervalMs;
            requestTimeoutMs = dvcLocalOptions.ConfigPollingTimeoutMs <= pollingIntervalMs
                ? pollingIntervalMs
                : dvcLocalOptions.ConfigPollingTimeoutMs;
            dvcLocalOptions.CdnCustomHeaders ??= new Dictionary<string, string>();

            DVCRestClientOptions clientOptions = restClientOptions?.Clone() ?? new DVCRestClientOptions();
            // Explicitly override the base URL to use the one in local bucketing options. This allows the normal 
            // rest client options to override the Events api endpoint url; while sharing certificate and other information.
            clientOptions.BaseUrl = new Uri(dvcLocalOptions.CdnUri);

            restClient = new RestClient(clientOptions);
            restClient.AddDefaultHeaders(dvcLocalOptions.CdnCustomHeaders);
            
            logger = loggerFactory.CreateLogger<EnvironmentConfigManager>();
            this.localBucketing = localBucketing;
            dvcEventArgs = new DVCEventArgs();

            if (initializedHandler != null)
            {
                this.initializedHandler += initializedHandler;
            }
        }

        public virtual async Task InitializeConfigAsync()
        {
            await FetchConfigAsyncWithTask();

            pollingTimer = new Timer(FetchConfigAsync, null, pollingIntervalMs, pollingIntervalMs);
        }

        public void Dispose()
        {
            pollingTimer?.Dispose();
            restClient.Dispose();
        }

        private void OnInitialized(DVCEventArgs e)
        {
            if (Initialized && alreadyCalledHandler) return;

            if (alreadyCalledHandler == false) {
                initializedHandler?.Invoke(this, e);
                alreadyCalledHandler = true;
            }
        }

        private string GetConfigUrl()
        {
            return localOptions.CdnSlug != "" ? localOptions.CdnSlug : $"/config/v1/server/{sdkKey}.json";
        }

        private void SetConfig(RestResponse res)
        {
            switch (res.StatusCode)
            {
                case HttpStatusCode.NotModified:
                    logger.LogInformation("Config not modified, using cache, etag: {ConfigEtag}", configEtag);
                    break;
                case HttpStatusCode.OK:
                {
                    var isInitialFetch = Config == null;
                    Config = res.Content;
                    localBucketing.StoreConfig(sdkKey, Config);

                    IEnumerable<HeaderParameter> headerValues = res.Headers.Where(e => e.Name.ToLower() == "etag");
                    configEtag = (string) headerValues.FirstOrDefault()?.Value;

                    logger.LogInformation("Config successfully initialized with etag: {ConfigEtag}", configEtag);

                    if (!isInitialFetch) return;

                    Initialized = true;
                    break;
                }
                default:
                {
                    if (Config != null)
                    {
                        logger.LogError("Failed to download config, using cached version: {ConfigEtag}", configEtag);
                    }
                    else
                    {
                        logger.LogError("Failed to download DevCycle config");

                        throw new DVCException(res.StatusCode,
                            new ErrorResponse("Failed to download DevCycle config."));
                    }

                    break;
                }
            }
        }

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

            try
            {
                RestResponse res = await ClientPolicy.GetInstance().RetryOncePolicy.ExecuteAsync(() => restClient.ExecuteAsync(request, cts.Token));
                dvcEventArgs.Success = true;
                SetConfig(res);
            }
            catch (DVCException e)
            {
                DVCException finalError;
                if (!e.IsRetryable())
                {
                    dvcEventArgs.Success = false;

                    if ((int) e.HttpStatusCode == 403)
                    {
                        finalError = new DVCException(e.HttpStatusCode,
                            new ErrorResponse("Project configuration could not be found. Check your SDK key."));
                    }
                    else
                    {
                        finalError = new DVCException(e.HttpStatusCode,
                            new ErrorResponse(
                                "Encountered non-retryable error fetching config. Halting polling loop."));
                    }

                    pollingTimer?.Dispose();
                    PollingEnabled = false;

                    logger.LogError(finalError.ErrorResponse.Message);
                    dvcEventArgs.Errors.Add(finalError);
                }
                else if (Config == null && configEtag == null)
                {
                    logger.LogDebug("Error loading initial config. Exception: " + e.Message);
                }
                else
                {
                    logger.LogDebug(String.Format(
                            "Error loading config. Using cache etag: {ConfigEtag}. Exception: {Exception}",
                            configEtag,
                            e.Message
                    ));
                }
            } catch (WasmtimeException e) {
                // This is to catch any exception that is thrown by the SetConfig method if the config is not valid
                logger.LogError($"Failed to set config: {e.InnerException?.Message}");
                dvcEventArgs.Errors.Add(new DVCException(new ErrorResponse(e.InnerException?.Message)));
            }
            finally
            {
                OnInitialized(dvcEventArgs);
            }
        }

        private async void FetchConfigAsync(object state = null)
        {
            await FetchConfigAsyncWithTask();
        }

        internal RestClient GetRestClient()
        {
            return restClient;
        }
    }
}