using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevCycle.SDK.Server.Common.API;
using DevCycle.SDK.Server.Common.Exception;
using DevCycle.SDK.Server.Common.Model;
using DevCycle.SDK.Server.Common.Model.Cloud;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;


namespace DevCycle.SDK.Server.Cloud.Api
{

    public class DVCCloudClientBuilder : DVCClientBuilder<DVCCloudClient, DVCCloudOptions, DVCCloudClientBuilder>
    {
        protected override DVCCloudClientBuilder BuilderInstance => this;

        public override DVCCloudClient Build()
        {
            return new DVCCloudClient(sdkKey, loggerFactory, options, restClientOptions);
        }
    }
    public sealed class DVCCloudClient : DVCBaseClient
    {
        private readonly DVCApiClient apiClient;
        private ILogger logger;

        private readonly DVCCloudOptions options;

        internal DVCCloudClient(
            string sdkKey,
            ILoggerFactory loggerFactory, 
            IDVCOptions options=null,
            DVCRestClientOptions restClientOptions = null
        ) {
            ValidateSDKKey(sdkKey);
            apiClient = new DVCApiClient(sdkKey, restClientOptions);
            logger = loggerFactory.CreateLogger<DVCCloudClient>();
            this.options = options != null ? (DVCCloudOptions) options : new DVCCloudOptions();
        }
        
        public override string Platform()
        {
            return "Cloud"; 
        }

        public override IDVCApiClient GetApiClient()
        {
            return apiClient;
        }

        public async Task<Dictionary<string, Feature>> AllFeaturesAsync(User user)
        {
            ValidateUser(user);

            AddDefaults(user);

            string urlFragment = "v1/features";
            var queryParams = new Dictionary<string, string>();
            if (options.EnableEdgeDB) queryParams.Add("enableEdgeDB", "true");

            try
            {
                return await GetResponseAsync<Dictionary<string, Feature>>(user, urlFragment, queryParams);

            }
            catch (DVCException e)
            {
                if (!e.IsRetryable() && (int)e.HttpStatusCode >= 400) {
                    throw e;
                }
                logger.LogError(e, "Failed to request AllFeatures:");
                return new Dictionary<string, Feature>();
            }
            
        }

        public async Task<T> VariableValueAsync<T>(User user, string key, T defaultValue)
        {
            return (await VariableAsync(user, key, defaultValue)).Value;
        }
        
        public async Task<Variable<T>> VariableAsync<T>(User user, string key, T defaultValue)
        {
            ValidateUser(user);

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("key cannot be null or empty");
            }

            if (defaultValue == null)
            {
                throw new ArgumentNullException(nameof(defaultValue));
            }

            AddDefaults(user);

            string lowerKey = key.ToLower();

            string urlFragment = "v1/variables/" + lowerKey;
            var queryParams = new Dictionary<string, string>();
            if (options.EnableEdgeDB) queryParams.Add("enableEdgeDB", "true");

            Variable<T> variable;

            TypeEnum type = Common.Model.Local.Variable<T>.DetermineType(defaultValue);
            
            try
            {
                var variableResponse = await GetResponseAsync<Variable<object>>(user, urlFragment, queryParams);
                variableResponse.DefaultValue = defaultValue;
                variableResponse.IsDefaulted = false;
                variableResponse.Type = Common.Model.Local.Variable<object>.DetermineType(variableResponse.Value);
               
                try
                {
                    variable = variableResponse.Convert<T>();
                }
                catch (InvalidCastException e)
                {
                    variable = new Variable<T>(lowerKey, defaultValue);
                    logger.LogWarning($"Type mismatch for variable {key}. " +
                                      $"Expected {type}, got {variableResponse.Type}");
                }
            }
            catch (DVCException e)
            {
                if (!e.IsRetryable() && (int)e.HttpStatusCode >= 400 && (int)e.HttpStatusCode != 404) {
                    throw e;
                } else {
                    logger.LogError(e, "Failed to retrieve variable value, using default.");
                }
                variable = new Variable<T>(lowerKey, defaultValue);
            }

            return variable;
        }

        public async Task<Dictionary<string, ReadOnlyVariable<object>>> AllVariablesAsync(User user)
        {
            ValidateUser(user);

            AddDefaults(user);

            string urlFragment = "v1/variables";
            var queryParams = new Dictionary<string, string>();
            if (options.EnableEdgeDB) queryParams.Add("enableEdgeDB", "true");


            try
            {
                return await GetResponseAsync<Dictionary<string, ReadOnlyVariable<object>>>(user, urlFragment,
                    queryParams);
            }
            catch (DVCException e)
            {
                if (!e.IsRetryable() && (int)e.HttpStatusCode >= 400) {
                    throw e;
                }
                logger.LogError(e, "Failed to request AllVariables");
                return new Dictionary<string, ReadOnlyVariable<object>>();
            }
        }

        public async Task<DVCResponse> TrackAsync(User user, Event userEvent)
        {
            ValidateUser(user);

            AddDefaults(user);

            string urlFragment = "v1/track";
            var queryParams = new Dictionary<string, string>();
            if (options.EnableEdgeDB) queryParams.Add("enableEdgeDB", "true");

            UserAndEvents userAndEvents = new UserAndEvents(new List<Event>() {userEvent}, user);

            try 
            {
                return await GetResponseAsync<DVCResponse>(userAndEvents, urlFragment, queryParams);
            } 
            catch (DVCException e)
            {
                if (!e.IsRetryable() && (int)e.HttpStatusCode >= 400) {
                    throw e;
                }
                logger.LogError(e, "Failed to request AllVariables");
                return new DVCResponse(e.ToString());
            }
        }

        public override void Dispose()
        {
            ((IDisposable) apiClient).Dispose();
        }
        
        /**
         * Rebuild the logging of the client and all subcomponents
         */
        public override void UpdateLogging(ILoggerFactory loggerFactory) {
            if (loggerFactory != null) {
                logger = loggerFactory.CreateLogger<DVCCloudClient>();
            }
        }
    }
}