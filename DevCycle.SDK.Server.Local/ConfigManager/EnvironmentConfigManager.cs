using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DevCycle.SDK.Server.Local.Api;
using DevCycle.SDK.Server.Common.Exception;
using DevCycle.SDK.Server.Common.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RestSharp.Portable;
using RestSharp.Portable.HttpClient;
using ErrorResponse = DevCycle.SDK.Server.Common.Model.ErrorResponse;

namespace DevCycle.SDK.Server.Local.ConfigManager
{
    public class EnvironmentConfigManager : IDisposable
    {
        private const int MinimumPollingIntervalMs = 1000;

        private readonly string environmentKey;
        private readonly int pollingIntervalMs;
        private readonly int requestTimeoutMs;
        private readonly IRestClient restClient;
        private readonly ILogger logger;
        private readonly ILocalBucketing localBucketing;
        private readonly DVCEventArgs dvcEventArgs;
        private readonly EventHandler<DVCEventArgs> initializedHandler;

        private Timer pollingTimer;

        public virtual string Config { get; private set; }
        public virtual bool Initialized { get; private set; }

        private bool PollingEnabled = true;

        private string configEtag;
        private bool alreadyCalledHandler;

        // internal parameterless constructor for testing
        internal EnvironmentConfigManager() : this("not-a-real-key", new DVCOptions(),
            new NullLoggerFactory(), new LocalBucketing())
        {
        }

        public EnvironmentConfigManager(string environmentKey, DVCOptions dvcOptions, ILoggerFactory loggerFactory,
            ILocalBucketing localBucketing, EventHandler<DVCEventArgs> initializedHandler = null)
        {
            this.environmentKey = environmentKey;
            
            pollingIntervalMs = dvcOptions.ConfigPollingIntervalMs >= MinimumPollingIntervalMs
                ? dvcOptions.ConfigPollingIntervalMs
                : MinimumPollingIntervalMs;
            requestTimeoutMs = dvcOptions.ConfigPollingTimeoutMs <= pollingIntervalMs
                ? pollingIntervalMs
                : dvcOptions.ConfigPollingTimeoutMs;

            restClient = new RestClient(dvcOptions.CdnUri);
            this.logger = loggerFactory.CreateLogger<EnvironmentConfigManager>();
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
            
            initializedHandler?.Invoke(this, e);

            if (Initialized)
            {
                alreadyCalledHandler = true;
            }
        }

        private string GetConfigUrl()
        {
            return $"/config/v1/server/{environmentKey}.json";
        }
        
        private void SetConfig(IRestResponse res)
        {
            if (res.StatusCode == HttpStatusCode.NotModified)
            {
                logger.LogInformation("Config not modified, using cache, etag: {ConfigEtag}", configEtag);
            }
            else if (res.StatusCode == HttpStatusCode.OK)
            {
                var isInitialFetch = Config == null;
                Config = res.Content;
                localBucketing.StoreConfig(environmentKey, Config);

                IEnumerable<string> headerValues = res.Headers.GetValues("etag");
                configEtag = new List<string>(headerValues).FirstOrDefault();

                logger.LogInformation("Config successfully initialized with etag: {ConfigEtag}", configEtag);

                if (!isInitialFetch) return;
                
                Initialized = true;
                dvcEventArgs.Success = true;
            }
            else if (Config != null)
            {
                logger.LogError("Failed to download config, using cached version: {ConfigEtag}", configEtag);
            }
            else
            {
                logger.LogError("Failed to download DevCycle config");

                var exception = new DVCException(res.StatusCode,
                    new ErrorResponse("Failed to download DevCycle config."));
                dvcEventArgs.Error = exception;

                throw exception;
            }
        }

        private async Task FetchConfigAsyncWithTask()
        {
            if (!PollingEnabled)
            {
                return;
            }
            
            restClient.IgnoreResponseStatusCode = true;
            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMilliseconds(requestTimeoutMs));
            var request = new RestRequest(GetConfigUrl(), Method.GET);
            if (configEtag != null) request.AddHeader("If-None-Match", configEtag);

            try
            {
                IRestResponse res = await restClient.Execute(request, cts.Token);
                SetConfig(res);
            }
            catch (DVCException e)
            {
                DVCException finalError;
                if (!e.IsRetryable())
                {
                    if ((int)e.HttpStatusCode == 403)
                    {
                        finalError = new DVCException(e.HttpStatusCode,
                            new ErrorResponse("Project configuration could not be found. Check your SDK key."));
                    }
                    else
                    {
                        finalError = new DVCException(e.HttpStatusCode,
                            new ErrorResponse("Encountered non-retryable error fetching config. Halting polling loop."));
                    }

                    pollingTimer?.Dispose();
                    PollingEnabled = false;
                }
                else if (Config == null && configEtag == null)
                {
                    finalError = new DVCException(e.HttpStatusCode,
                        new ErrorResponse("Error loading initial config. Exception: " + e.Message));
                } 
                else
                {
                    finalError = new DVCException(e.HttpStatusCode,
                        new ErrorResponse(String.Format(
                            "Error loading config. Using cache etag: {ConfigEtag}. Exception: {Exception}",
                            configEtag, 
                            e.Message
                        )));
                }
                logger.LogError(finalError.ErrorResponse.Message);
                dvcEventArgs.Error = finalError;
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
    }
}