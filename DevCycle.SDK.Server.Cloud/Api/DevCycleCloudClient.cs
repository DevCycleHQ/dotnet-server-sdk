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

        public override DevCycleProvider BuildOpenFeatureProvider()
        {
            return Build().GetOpenFeatureProvider();
        }
    }

    public sealed class DevCycleCloudClient : DevCycleBaseClient
    {
        private readonly DevCycleApiClient apiClient;
        private readonly ILogger logger;
        private DevCycleProvider OpenFeatureProvider { get; }
        private readonly DevCycleCloudOptions options;
        private readonly EvalHooksRunner evalHooksRunner;

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
            OpenFeatureProvider = new DevCycleProvider(this);
            evalHooksRunner = new EvalHooksRunner(logger);
        }

        public override string Platform()
        {
            return "Cloud";
        }

        public override IDevCycleApiClient GetApiClient()
        {
            return apiClient;
        }

        public override DevCycleProvider GetOpenFeatureProvider()
        {
            return OpenFeatureProvider;
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

            Variable<T> variable = new Variable<T>(lowerKey, defaultValue);

            TypeEnum type = Common.Model.Variable<T>.DetermineType(defaultValue);
            HookContext<T> hookContext = new HookContext<T>(user, key, defaultValue, null);

            var hooks = evalHooksRunner.GetHooks();
            var reversedHooks = new List<EvalHook>(hooks);
            reversedHooks.Reverse();
            try
            {
                BeforeHookError beforeHookError = null;
                try
                {
                    hookContext = await evalHooksRunner.RunBeforeAsync(hooks, hookContext);
                }
                catch (BeforeHookError e)
                {
                    beforeHookError = e;
                }
                var variableResponse = await GetResponseAsync<Variable<object>>(user, urlFragment, queryParams);
                variableResponse.DefaultValue = defaultValue;
                variableResponse.IsDefaulted = false;
                variableResponse.Type = Common.Model.Variable<object>.DetermineType(variableResponse.Value);

                try
                {
                    variable = variableResponse.Convert<T>();
                }
                catch (InvalidCastException)
                {
                    variable = new Variable<T>(lowerKey, defaultValue);
                    logger.LogWarning($"Type mismatch for variable {key}. " +
                                      $"Expected {type}, got {variableResponse.Type}");
                }

                if (beforeHookError != null)
                {
                    throw beforeHookError;
                }
                await evalHooksRunner.RunAfterAsync(reversedHooks, hookContext, variable);
            }
            catch (Exception e)
            {
                if (e is DevCycleException devCycleException)
                {
                    if (!devCycleException.IsRetryable() && (int)devCycleException.HttpStatusCode >= 400 && (int)devCycleException.HttpStatusCode != 404)
                    {
                        throw;
                    }
                }

                if (!(e is BeforeHookError) && !(e is AfterHookError))
                {
                    logger.LogError(e, "Failed to retrieve variable value, using default.");
                    variable = new Variable<T>(lowerKey, defaultValue);
                }
                await evalHooksRunner.RunErrorAsync(reversedHooks, hookContext, e);
            } finally
            {
                await evalHooksRunner.RunFinallyAsync(reversedHooks, hookContext, variable);
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

        public void AddEvalHook(EvalHook hook)
        {
            evalHooksRunner.AddHook(hook);
        }

        public void ClearEvalHooks()
        {
            evalHooksRunner.ClearHooks();
        }

        public override void Dispose()
        {
            ((IDisposable)apiClient).Dispose();
        }
    }
}