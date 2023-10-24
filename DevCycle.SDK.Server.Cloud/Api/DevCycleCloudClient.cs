using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevCycle.SDK.Server.Common.API;
using DevCycle.SDK.Server.Common.Exception;
using DevCycle.SDK.Server.Common.Model;
using DevCycle.SDK.Server.Common.Model.Cloud;
using Microsoft.Extensions.Logging;

namespace DevCycle.SDK.Server.Cloud.Api
{
    public class DevCycleCloudClientBuilder : DevCycleClientBuilder<DevCycleCloudClient, DevCycleCloudOptions,
        DevCycleCloudClientBuilder>
    {
        protected override DevCycleCloudClientBuilder BuilderInstance => this;

        public override DevCycleCloudClient Build()
        {
            return new DevCycleCloudClient(sdkKey, loggerFactory, options, restClientOptions);
        }
    }

    public sealed class DevCycleCloudClient : DevCycleBaseClient
    {
        private readonly DevCycleApiClient apiClient;
        private readonly ILogger logger;

        private readonly DevCycleCloudOptions options;

        internal DevCycleCloudClient(
            string sdkKey,
            ILoggerFactory loggerFactory,
            IDevCycleOptions options = null,
            DevCycleRestClientOptions restClientOptions = null
        )
        {
            ValidateSDKKey(sdkKey);
            apiClient = new DevCycleApiClient(sdkKey, restClientOptions);
            logger = loggerFactory.CreateLogger<DevCycleCloudClient>();
            this.options = options != null ? (DevCycleCloudOptions)options : new DevCycleCloudOptions();
        }

        public override string Platform()
        {
            return "Cloud";
        }

        public override IDevCycleApiClient GetApiClient()
        {
            return apiClient;
        }

        public override async Task<Dictionary<string, Feature>> AllFeatures(DevCycleUser user)
        {
            ValidateUser(user);

            AddDefaults(user);

            const string urlFragment = "v1/features";
            var queryParams = new Dictionary<string, string>();
            if (options.EnableEdgeDB) queryParams.Add("enableEdgeDB", "true");

            try
            {
                return await GetResponseAsync<Dictionary<string, Feature>>(user, urlFragment, queryParams);
            }
            catch (DevCycleException e)
            {
                if (!e.IsRetryable() && (int)e.HttpStatusCode >= 400)
                {
                    throw e;
                }

                logger.LogError(e, "Failed to request AllFeatures:");
                return new Dictionary<string, Feature>();
            }
        }

        public override async Task<T> VariableValue<T>(DevCycleUser user, string key, T defaultValue)
        {
            return (await Variable(user, key, defaultValue)).Value;
        }

        public override async Task<Variable<T>> Variable<T>(DevCycleUser user, string key, T defaultValue)
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

            TypeEnum type = Common.Model.Variable<T>.DetermineType(defaultValue);

            try
            {
                var variableResponse = await GetResponseAsync<Variable<object>>(user, urlFragment, queryParams);
                variableResponse.DefaultValue = defaultValue;
                variableResponse.IsDefaulted = false;
                variableResponse.Type = Common.Model.Variable<object>.DetermineType(variableResponse.Value);

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
            catch (DevCycleException e)
            {
                if (!e.IsRetryable() && (int)e.HttpStatusCode >= 400 && (int)e.HttpStatusCode != 404)
                {
                    throw;
                }

                logger.LogError(e, "Failed to retrieve variable value, using default.");
                variable = new Variable<T>(lowerKey, defaultValue);
            }

            return variable;
        }

        public override async Task<Dictionary<string, ReadOnlyVariable<object>>> AllVariables(DevCycleUser user)
        {
            ValidateUser(user);

            AddDefaults(user);

            const string urlFragment = "v1/variables";
            var queryParams = new Dictionary<string, string>();
            if (options.EnableEdgeDB) queryParams.Add("enableEdgeDB", "true");


            try
            {
                return await GetResponseAsync<Dictionary<string, ReadOnlyVariable<object>>>(user, urlFragment,
                    queryParams);
            }
            catch (DevCycleException e)
            {
                if (!e.IsRetryable() && (int)e.HttpStatusCode >= 400)
                {
                    throw e;
                }

                logger.LogError(e, "Failed to request AllVariables");
                return new Dictionary<string, ReadOnlyVariable<object>>();
            }
        }

        public override async Task<DevCycleResponse> Track(DevCycleUser user, DevCycleEvent userEvent)
        {
            ValidateUser(user);

            AddDefaults(user);

            string urlFragment = "v1/track";
            var queryParams = new Dictionary<string, string>();
            if (options.EnableEdgeDB) queryParams.Add("enableEdgeDB", "true");

            UserAndEvents userAndEvents = new UserAndEvents(new List<DevCycleEvent> { userEvent }, user);

            try
            {
                return await GetResponseAsync<DevCycleResponse>(userAndEvents, urlFragment, queryParams);
            }
            catch (DevCycleException e)
            {
                if (!e.IsRetryable() && (int)e.HttpStatusCode >= 400)
                {
                    throw e;
                }

                logger.LogError(e, "Failed to request AllVariables");
                return new DevCycleResponse(e.ToString());
            }
        }

        public override void Dispose()
        {
            ((IDisposable)apiClient).Dispose();
        }
    }
}