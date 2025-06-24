using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using DevCycle.SDK.Server.Common.API;
using DevCycle.SDK.Server.Common.Model;
using DevCycle.SDK.Server.Common.Model.Local;
using DevCycle.SDK.Server.Local.ConfigManager;
using DevCycle.SDK.Server.Local.Protobuf;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using DevCycle.SDK.Server.Common.Model;
using System.Runtime.InteropServices;

namespace DevCycle.SDK.Server.Local.Api
{
    public class DevCycleLocalClientBuilder : DevCycleClientBuilder<DevCycleLocalClient, DevCycleLocalOptions,
        DevCycleLocalClientBuilder>
    {
        private EnvironmentConfigManager configManager;
        private LocalBucketing localBucketing;

        protected override DevCycleLocalClientBuilder BuilderInstance => this;

        public DevCycleLocalClientBuilder SetConfigManager(EnvironmentConfigManager environmentConfigManager)
        {
            configManager = environmentConfigManager;
            return this;
        }

        public DevCycleLocalClientBuilder SetLocalBucketing(LocalBucketing localBucketingWrapper)
        {
            localBucketing = localBucketingWrapper;
            return this;
        }

        public DevCycleLocalClientBuilder SetInitializedSubscriber(
            EventHandler<DevCycleEventArgs> initializedEventHandler)
        {
            initialized = initializedEventHandler;
            return this;
        }

        public override DevCycleLocalClient Build()
        {
            localBucketing ??= new LocalBucketing();

            options ??= new DevCycleLocalOptions();

            loggerFactory ??= LoggerFactory.Create(builder => builder.AddSimpleConsole(options =>
            {
                options.IncludeScopes = false;
                options.SingleLine = true;
            }));

            configManager ??= new EnvironmentConfigManager(sdkKey, options, loggerFactory, localBucketing,
                initialized, restClientOptions);

            return new DevCycleLocalClient(sdkKey, options, loggerFactory, configManager, localBucketing,
                restClientOptions);
        }

        public override DevCycleProvider BuildOpenFeatureProvider()
        {
            return Build().GetOpenFeatureProvider();
        }
    }

    public class DevCycleLocalClient : DevCycleBaseClient
    {
        private readonly string sdkKey;
        private readonly EnvironmentConfigManager configManager;
        private readonly EventQueue eventQueue;
        private readonly LocalBucketing localBucketing;
        private readonly ILogger logger;
        private readonly Timer timer;
        private bool closing;
        private DevCycleProvider OpenFeatureProvider { get; }
        private readonly EvalHooksRunner evalHooksRunner;

        internal DevCycleLocalClient(
            string sdkKey,
            DevCycleLocalOptions dvcLocalOptions,
            ILoggerFactory loggerFactory,
            EnvironmentConfigManager configManager,
            LocalBucketing localBucketing,
            DevCycleRestClientOptions restClientOptions = null
        )
        {
            ValidateSDKKey(sdkKey);
            this.sdkKey = sdkKey;
            this.configManager = configManager;
            this.localBucketing = localBucketing;
            logger = loggerFactory.CreateLogger<DevCycleLocalClient>();
            eventQueue = new EventQueue(sdkKey, dvcLocalOptions, loggerFactory, localBucketing, restClientOptions);
            this.configManager.SetEventQueue(eventQueue);
            var platformData = new PlatformData();
            localBucketing.SetPlatformData(platformData.ToJson());
            evalHooksRunner = new EvalHooksRunner(logger);

            if(dvcLocalOptions.CdnSlug != "")
            {
                logger.LogWarning("The config CDN slug is being overriden, please ensure to update the config to v2 according to the config CDN updates documentation.");

            }
            timer = new Timer(dvcLocalOptions.EventFlushIntervalMs);
            timer.Elapsed += OnTimedEvent;
            timer.AutoReset = true;
            timer.Enabled = true;
            Task.Run(async delegate { await this.configManager.InitializeConfigAsync(); });
            OpenFeatureProvider = new DevCycleProvider(this);
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            eventQueue?.ScheduleFlush();
        }

        public void AddEvalHook(EvalHook hook)
        {
            evalHooksRunner.AddHook(hook);
        }

        public void ClearEvalHooks()
        {
            evalHooksRunner.ClearHooks();
        }

        private NullableString CreateNullableString(string value)
        {
            return value == null
                ? new NullableString() { IsNull = true }
                : new NullableString() { IsNull = false, Value = value };
        }

        private NullableDouble CreateNullableDouble(double value)
        {
            return !Double.IsNaN(value)
                ? new NullableDouble() { IsNull = false, Value = value }
                : new NullableDouble() { IsNull = true };
        }

        private NullableCustomData CreateNullableCustomData(Dictionary<string, object> customData)
        {
            if (customData == null)
            {
                return new NullableCustomData() { IsNull = true };
            }
            else
            {
                NullableCustomData nullableCustomData = new NullableCustomData() { IsNull = false };
                foreach (KeyValuePair<string, object> entry in customData)
                {
                    if (entry.Value == null)
                    {
                        nullableCustomData.Value[entry.Key] = new CustomDataValue() { Type = CustomDataType.Null };
                    }
                    else if (entry.Value is string strValue)
                    {
                        nullableCustomData.Value[entry.Key] = new CustomDataValue()
                            { StringValue = strValue, Type = CustomDataType.Str };
                    }
                    else if (entry.Value is double numValue)
                    {
                        nullableCustomData.Value[entry.Key] = new CustomDataValue()
                            { DoubleValue = numValue, Type = CustomDataType.Num };
                    }
                    else if (entry.Value is bool boolValue)
                    {
                        nullableCustomData.Value[entry.Key] = new CustomDataValue()
                            { BoolValue = boolValue, Type = CustomDataType.Bool };
                    }
                }

                return nullableCustomData;
            }
        }

        private VariableType_PB TypeEnumToVariableTypeProtobuf(TypeEnum type)
        {
            switch (type)
            {
                case TypeEnum.Boolean:
                    return VariableType_PB.Boolean;
                case TypeEnum.String:
                    return VariableType_PB.String;
                case TypeEnum.Number:
                    return VariableType_PB.Number;
                case TypeEnum.JSON:
                    return VariableType_PB.Json;
                default:
                    throw new ArgumentOutOfRangeException("Unknown variable type: " + type);
            }
        }

        public void SetClientCustomData(Dictionary<string, object> customData)
        {
            if (!configManager.Initialized)
            {
                logger.LogWarning("SetClientCustomData called before DevCycleClient has initialized");
                return;
            }

            string data = JsonConvert.SerializeObject(customData);
            try
            {
                localBucketing.SetClientCustomData(sdkKey, data);
            }
            catch (Exception e)
            {
                logger.LogError("Unexpected exception setting client custom data {Exception}", e.Message);
            }
        }

        public void AddFlushedEventSubscriber(EventHandler<DevCycleEventArgs> flushedEventsSubscriber)
        {
            eventQueue.AddFlushedEventsSubscriber(flushedEventsSubscriber);
        }

        public void RemoveFlushedEventsSubscriber(EventHandler<DevCycleEventArgs> flushedEventsSubscriber)
        {
            eventQueue.RemoveFlushedEventsSubscriber(flushedEventsSubscriber);
        }

        /**
         * FlushEvents will immediately push all queued events to the DevCycle servers. To subscribe to success/failure
         * notifications attach an event handler to FlushedEvents.
         */
        public void FlushEvents()
        {
            _ = eventQueue.FlushEvents();
        }

        public override void Dispose()
        {
            closing = true;
            eventQueue.Dispose();
            configManager.Dispose();
            timer.Dispose();
        }

        public override string Platform()
        {
            return "Local";
        }

        public override IDevCycleApiClient GetApiClient()
        {
            throw new NotImplementedException();
        }

        public override DevCycleProvider GetOpenFeatureProvider()
        {
            return OpenFeatureProvider;
        }

        public override Task<Dictionary<string, Feature>> AllFeatures(DevCycleUser user)
        {
            if (!configManager.Initialized)
            {
                logger.LogWarning("AllFeatures called before DevCycleClient has initialized");
                return Task.FromResult(new Dictionary<string, Feature>());
            }

            var requestUser = new DevCyclePopulatedUser(user);

            try
            {
                var config = localBucketing.GenerateBucketedConfig(sdkKey, requestUser.ToJson());
                return Task.FromResult(config.Features);
            }
            catch (Exception e)
            {
                logger.LogError("Unexpected exception retrieving features from the config {Exception}", e.Message);
                return Task.FromResult(new Dictionary<string, Feature>());
            }
        }

        public override Task<Dictionary<string, ReadOnlyVariable<object>>> AllVariables(DevCycleUser user)
        {
            if (!configManager.Initialized)
            {
                logger.LogWarning("AllVariables called before DevCycleClient has initialized");
                return Task.FromResult(new Dictionary<string, ReadOnlyVariable<object>>());
            }

            var requestUser = new DevCyclePopulatedUser(user);

            try
            {
                var config = localBucketing.GenerateBucketedConfig(sdkKey, requestUser.ToJson());
                return Task.FromResult(config.Variables);
            }
            catch (Exception e)
            {
                logger.LogError("Unexpected exception retrieving variables from the config: {Exception}", e.Message);
                return Task.FromResult(new Dictionary<string, ReadOnlyVariable<object>>());
            }
        }

        public override Task<Variable<T>> Variable<T>(DevCycleUser user, string key, T defaultValue)
        {
            var requestUser = new DevCyclePopulatedUser(user);

            if (!configManager.Initialized)
            {
                logger.LogWarning("Variable called before DevCycleClient has initialized, returning default value");

                eventQueue.QueueAggregateEvent(
                    requestUser,
                    new DevCycleEvent(type: EventTypes.aggVariableDefaulted, target: key),
                    null
                );
                return Task.FromResult(Common.Model.Variable<T>.InitializeFromVariable(null, key, defaultValue));
            }

            var userPb = GetDevCycleUser_PB(user);

            var type = Common.Model.Variable<T>.DetermineType(defaultValue);
            VariableType_PB variableType = TypeEnumToVariableTypeProtobuf(type);

            VariableForUserParams_PB paramsPb = new VariableForUserParams_PB()
            {
                SdkKey = sdkKey,
                User = userPb,
                VariableKey = key,
                VariableType = variableType,
                ShouldTrackEvent = true
            };

            Variable<T> existingVariable;
            try
            {
                var paramsBuffer = paramsPb.ToByteArray();

                byte[] variableData = localBucketing.GetVariableForUserProtobuf(serializedParams: paramsBuffer);

                if (variableData == null)
                {
                    return Task.FromResult(Common.Model.Variable<T>.InitializeFromVariable(null, key, defaultValue));
                }

                SDKVariable_PB sdkVariable = SDKVariable_PB.Parser.ParseFrom(variableData);

                if (variableType != sdkVariable.Type)
                {
                    logger.LogWarning("Type of Variable does not match DevCycle configuration. Using default value");
                    return Task.FromResult(Common.Model.Variable<T>.InitializeFromVariable(null, key, defaultValue));
                }

                existingVariable = GetVariable<T>(sdkVariable, defaultValue);
            }
            catch (Exception e)
            {
                logger.LogError("Unexpected exception getting variable: {Exception}", e.Message);
                return Task.FromResult(Common.Model.Variable<T>.InitializeFromVariable(null, key, defaultValue));
            }

            return Task.FromResult(existingVariable);
        }

        public async Task<Variable<T>> VariableAsync<T>(DevCycleUser user, string key, T defaultValue)
        {
            var requestUser = new DevCyclePopulatedUser(user);

            if (!configManager.Initialized)
            {
                logger.LogWarning("Variable called before DevCycleClient has initialized, returning default value");

                eventQueue.QueueAggregateEvent(
                    requestUser,
                    new DevCycleEvent(type: EventTypes.aggVariableDefaulted, target: key),
                    null
                );
                return Common.Model.Variable<T>.InitializeFromVariable(null, key, defaultValue);
            }

            var userPb = GetDevCycleUser_PB(user);

            var type = Common.Model.Variable<T>.DetermineType(defaultValue);
            VariableType_PB variableType = TypeEnumToVariableTypeProtobuf(type);

            VariableForUserParams_PB paramsPb = new VariableForUserParams_PB()
            {
                SdkKey = sdkKey,
                User = userPb,
                VariableKey = key,
                VariableType = variableType,
                ShouldTrackEvent = true
            };

            Variable<T> existingVariable = Common.Model.Variable<T>.InitializeFromVariable(null, key, defaultValue);;
            HookContext<T> hookContext = new HookContext<T>(user, key, defaultValue, null);
            
            var hooks = evalHooksRunner.GetHooks();
            var reversedHooks = new List<EvalHook>(hooks);
            reversedHooks.Reverse();
            
            try
            {
                System.Exception beforeError = null;
                try
                {
                    hookContext = await evalHooksRunner.RunBeforeAsync(hooks, hookContext);
                }
                catch (System.Exception e)
                {
                    beforeError = e;
                }
                
                var paramsBuffer = paramsPb.ToByteArray();

                byte[] variableData = localBucketing.GetVariableForUserProtobuf(serializedParams: paramsBuffer);

                if (variableData == null)
                {
                    logger.LogWarning("Variable data is null, using default value");
                    await evalHooksRunner.RunAfterAsync(reversedHooks, hookContext, existingVariable);
                    await evalHooksRunner.RunFinallyAsync(reversedHooks, hookContext, existingVariable);
                    return existingVariable;
                }

                SDKVariable_PB sdkVariable = SDKVariable_PB.Parser.ParseFrom(variableData);

                if (variableType != sdkVariable.Type)
                {
                    logger.LogWarning("Type of Variable does not match DevCycle configuration. Using default value");
                } else {
                    existingVariable = GetVariable<T>(sdkVariable, defaultValue);
                }

                if (beforeError != null)
                {
                    throw beforeError;
                }
                await evalHooksRunner.RunAfterAsync(reversedHooks, hookContext, existingVariable);
            }
            catch (Exception e)
            {
                if (e is not BeforeHookError && e is not AfterHookError)
                {
                    logger.LogError("Unexpected exception getting variable: {Exception}", e.Message);
                }
                await evalHooksRunner.RunErrorAsync(reversedHooks, hookContext, e);
            }
            finally
            {
                await evalHooksRunner.RunFinallyAsync(reversedHooks, hookContext, existingVariable);
            }
            return existingVariable;
        }

    

        public override async Task<T> VariableValue<T>(DevCycleUser user, string key, T defaultValue)
        {
            return (await Variable(user, key, defaultValue)).Value;
        }

        public override Task<DevCycleResponse> Track(DevCycleUser user, DevCycleEvent userEvent)
        {
            if (closing)
            {
                logger.LogError("Client is closing, can not track new events");
                return Task.FromResult(new DevCycleResponse("Client is closing, can not track new events"));
            }

            var requestUser = new DevCyclePopulatedUser(user);

            if (!configManager.Initialized)
            {
                logger.LogWarning("Track called before DevCycleClient has initialized");
            }
            else
            {
                localBucketing.GenerateBucketedConfig(sdkKey, requestUser.ToJson());
            }

            try
            {
                eventQueue.QueueEvent(requestUser, userEvent);
                return Task.FromResult(new DevCycleResponse("Successfully Queued Event"));
            }
            catch (Exception e)
            {
                var message = $"Unexpected exception queueing event: {e.Message}";
                logger.LogError(message);
                return Task.FromResult(new DevCycleResponse(message));
            }
        }

        private DVCUser_PB GetDevCycleUser_PB(DevCycleUser user)
        {
            DVCUser_PB userPb = new DVCUser_PB()
            {
                UserId = user.UserId,
                Email = CreateNullableString(user.Email),
                Name = CreateNullableString(user.Name),
                Language = CreateNullableString(user.Language),
                Country = CreateNullableString(user.Country),
                AppBuild = CreateNullableDouble(user.AppBuild),
                AppVersion = CreateNullableString(user.AppVersion),
                DeviceModel = CreateNullableString(user.DeviceModel),
                CustomData = CreateNullableCustomData(user.CustomData),
                PrivateCustomData = CreateNullableCustomData(user.PrivateCustomData)
            };
            return userPb;
        }

        private Variable<T> GetVariable<T>(SDKVariable_PB sdkVariable, T defaultValue)
        {
            Variable<T> existingVariable;
            switch (sdkVariable.Type)
            {
                case VariableType_PB.Boolean:
                    existingVariable = new Variable<T>(key: sdkVariable.Key,
                        value: (T)Convert.ChangeType(sdkVariable.BoolValue, typeof(T)), defaultValue: defaultValue);
                    break;
                case VariableType_PB.Number:
                    existingVariable = new Variable<T>(key: sdkVariable.Key,
                        value: (T)Convert.ChangeType(sdkVariable.DoubleValue, typeof(T)),
                        defaultValue: defaultValue);
                    break;
                case VariableType_PB.String:
                    existingVariable = new Variable<T>(key: sdkVariable.Key,
                        value: (T)Convert.ChangeType(sdkVariable.StringValue, typeof(T)),
                        defaultValue: defaultValue);
                    break;
                case VariableType_PB.Json:
                    // T is expected to be a JObject or JArray 
                    var jsonObj = JsonConvert.DeserializeObject<T>(sdkVariable.StringValue);
                    existingVariable = new Variable<T>(key: sdkVariable.Key, value: jsonObj,
                        defaultValue: defaultValue);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Unknown variable type: " + sdkVariable.Type);
            }
            return existingVariable;
        }
    }
}