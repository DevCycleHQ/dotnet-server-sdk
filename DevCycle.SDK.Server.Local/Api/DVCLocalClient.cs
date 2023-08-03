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


namespace DevCycle.SDK.Server.Local.Api
{
    public class DVCLocalClientBuilder : DVCClientBuilder<DVCLocalClient, DVCLocalOptions, DVCLocalClientBuilder>
    {
        private EnvironmentConfigManager configManager;
        private LocalBucketing localBucketing;
        
        protected override DVCLocalClientBuilder BuilderInstance => this;

        public DVCLocalClientBuilder SetConfigManager(EnvironmentConfigManager environmentConfigManager)
        {
            configManager = environmentConfigManager;
            return this;
        }

        public DVCLocalClientBuilder SetLocalBucketing(LocalBucketing localBucketingWrapper)
        {
            localBucketing = localBucketingWrapper;
            return this;
        }

        public DVCLocalClientBuilder SetInitializedSubscriber(EventHandler<DVCEventArgs> initializedEventHandler)
        {
            initialized = initializedEventHandler;
            return this;
        }

        public override DVCLocalClient Build()
        {
            loggerFactory ??= LoggerFactory.Create(builder => builder.AddConsole());

            localBucketing ??= new LocalBucketing(loggerFactory);

            options ??= new DVCLocalOptions();

            configManager ??= new EnvironmentConfigManager(sdkKey, options, loggerFactory, localBucketing,
                initialized, restClientOptions);

            return new DVCLocalClient(sdkKey, options, loggerFactory, configManager, localBucketing,
                restClientOptions);
        }
    }

    public sealed class DVCLocalClient : DVCBaseClient
    {
        private readonly string sdkKey;
        private readonly EnvironmentConfigManager configManager;
        private readonly EventQueue eventQueue;
        private readonly LocalBucketing localBucketing;
        private ILogger logger;
        private readonly Timer timer;
        private bool closing;

        internal DVCLocalClient(
            string sdkKey, 
            DVCLocalOptions dvcLocalOptions, 
            ILoggerFactory loggerFactory,
            EnvironmentConfigManager configManager, 
            LocalBucketing localBucketing,
            DVCRestClientOptions restClientOptions = null
        )  {
            ValidateSDKKey(sdkKey);
            this.sdkKey = sdkKey;
            this.configManager = configManager;
            this.localBucketing = localBucketing;
            logger = loggerFactory.CreateLogger<DVCLocalClient>();
            eventQueue = new EventQueue(sdkKey, dvcLocalOptions, loggerFactory, localBucketing, restClientOptions);

            var platformData = new PlatformData();
            localBucketing.SetPlatformData(platformData.ToJson());

            timer = new Timer(dvcLocalOptions.EventFlushIntervalMs);
            timer.Elapsed += OnTimedEvent;
            timer.AutoReset = true;
            timer.Enabled = true;
            Task.Run(async delegate { await this.configManager.InitializeConfigAsync(); });
        }
        
        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            eventQueue?.ScheduleFlush();
        }
        
        
        private NullableString CreateNullableString(string value)
        {
            return value == null ? new NullableString(){IsNull = true} : new NullableString() { IsNull = false, Value = value };
        }

        private NullableDouble CreateNullableDouble(double value)
        {
            return !Double.IsNaN(value) ? new NullableDouble() { IsNull = false, Value = value } : new NullableDouble() { IsNull = true };
        }
        
        private NullableCustomData CreateNullableCustomData(Dictionary<string, object> customData)
        {
            if (customData == null)
            {
                return new NullableCustomData() { IsNull = true };
            }
            else
            {
                NullableCustomData nullableCustomData = new NullableCustomData() { IsNull = false};
                foreach(KeyValuePair<string, object> entry in customData)
                {
                    if(entry.Value == null)
                    {
                        nullableCustomData.Value[entry.Key] = new CustomDataValue() { Type = CustomDataType.Null };
                    }
                    else if (entry.Value is string strValue)
                    {
                        nullableCustomData.Value[entry.Key] = new CustomDataValue() { StringValue = strValue, Type = CustomDataType.Str };
                    }
                    else if (entry.Value is double numValue)
                    {
                        nullableCustomData.Value[entry.Key] = new CustomDataValue() { DoubleValue = numValue, Type = CustomDataType.Num };
                    }
                    else if (entry.Value is bool boolValue)
                    {
                        nullableCustomData.Value[entry.Key] = new CustomDataValue() { BoolValue = boolValue, Type = CustomDataType.Bool };
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
                    throw new ArgumentOutOfRangeException("Unknown variable type: "+type);
            }
        }

        public T VariableValue<T>(User user, string key, T defaultValue) {
            return Variable(user, key, defaultValue).Value;
        }
        
        public Variable<T> Variable<T>(User user, string key, T defaultValue) {
            var requestUser = new DVCPopulatedUser(user);
            
            if (!configManager.Initialized)
            {
                logger.LogWarning("Variable called before DVCClient has initialized, returning default value");
                
                eventQueue.QueueAggregateEvent(
                    requestUser,
                    new Event(type: EventTypes.aggVariableDefaulted, target: key),
                    null
                );
                return Common.Model.Local.Variable<T>.InitializeFromVariable(null, key, defaultValue);
            }
            
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

            var type = Common.Model.Local.Variable<T>.DetermineType(defaultValue);
            VariableType_PB variableType = TypeEnumToVariableTypeProtobuf(type);
            
            VariableForUserParams_PB paramsPb = new VariableForUserParams_PB()
            {
                SdkKey = sdkKey,
                User = userPb,
                VariableKey = key,
                VariableType = variableType,
                ShouldTrackEvent = true
            };
            
            Variable<T> existingVariable = null;
            try
            {
                var paramsBuffer = paramsPb.ToByteArray();
            
                byte[] variableData = localBucketing.GetVariableForUserProtobuf(serializedParams:paramsBuffer);
            
                if (variableData == null)
                {
                    return Common.Model.Local.Variable<T>.InitializeFromVariable(null, key, defaultValue);
                }
            
                SDKVariable_PB sdkVariable = SDKVariable_PB.Parser.ParseFrom(variableData);
            
                if(variableType != sdkVariable.Type)
                {
                    logger.LogWarning("Type of Variable does not match DevCycle configuration. Using default value");
                    return Common.Model.Local.Variable<T>.InitializeFromVariable(null, key, defaultValue);
                }
            
                switch (sdkVariable.Type)
                {
                    case VariableType_PB.Boolean:
                        existingVariable = new Variable<T>(key: sdkVariable.Key, value: (T)Convert.ChangeType(sdkVariable.BoolValue, typeof(T)), defaultValue: defaultValue);
                        break;
                    case VariableType_PB.Number:
                        existingVariable = new Variable<T>(key: sdkVariable.Key, value: (T)Convert.ChangeType(sdkVariable.DoubleValue, typeof(T)), defaultValue: defaultValue);
                        break;
                    case VariableType_PB.String:
                        existingVariable = new Variable<T>(key: sdkVariable.Key, value: (T)Convert.ChangeType(sdkVariable.StringValue, typeof(T)), defaultValue: defaultValue);
                        break;
                    case VariableType_PB.Json:
                        // T is expected to be a JObject or JArray 
                        var jsonObj = JsonConvert.DeserializeObject<T>(sdkVariable.StringValue);
                        existingVariable = new Variable<T>(key: sdkVariable.Key, value: jsonObj, defaultValue: defaultValue);
                        break;  
                    default:
                        throw new ArgumentOutOfRangeException("Unknown variable type: "+sdkVariable.Type);
                }
            }
            catch (Exception e)
            {
                logger.LogError("Unexpected exception getting variable: {Exception}", e.Message);
                return Common.Model.Local.Variable<T>.InitializeFromVariable(null, key, defaultValue);
            }
            return existingVariable;
        }
        
        public Dictionary<string, Feature> AllFeatures(User user)
        {
            if (!configManager.Initialized)
            {
                logger.LogWarning("AllFeatures called before DVCClient has initialized");
                return new Dictionary<string, Feature>();
            }

            var requestUser = new DVCPopulatedUser(user);

            try
            {
                var config = localBucketing.GenerateBucketedConfig(sdkKey, requestUser.ToJson());
                return config.Features;
            }
            catch (Exception e)
            {
                logger.LogError("Unexpected exception retrieving features from the config {Exception}", e.Message);
                return new Dictionary<string, Feature>();
            }
        }

        public Dictionary<string, ReadOnlyVariable<object>> AllVariables(User user)
        {
            if (!configManager.Initialized)
            {
                logger.LogWarning("AllVariables called before DVCClient has initialized");
                return new Dictionary<string, ReadOnlyVariable<object>>();
            }

            var requestUser = new DVCPopulatedUser(user);

            try
            {
                var config = localBucketing.GenerateBucketedConfig(sdkKey, requestUser.ToJson());
                return config.Variables;
            }
            catch (Exception e)
            {
                logger.LogError("Unexpected exception retrieving variables from the config: {Exception}", e.Message);
                return new Dictionary<string, ReadOnlyVariable<object>>();
            }
        }

        public void SetClientCustomData(Dictionary<string,object> customData)
        {
            if (!configManager.Initialized)
            {
                logger.LogWarning("SetClientCustomData called before DVCClient has initialized");
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
        
        public void Track(User user, Event userEvent)
        {
            if (closing)
            {
                logger.LogError("Client is closing, can not track new events");
                return;
            }
            BucketedUserConfig config = null;
            var requestUser = new DVCPopulatedUser(user);

            if (!configManager.Initialized)
            {
                logger.LogWarning("Track called before DVCClient has initialized");
            }
            else
            {
                config = localBucketing.GenerateBucketedConfig(sdkKey, requestUser.ToJson());
            }


            try
            {
                eventQueue.QueueEvent(requestUser, userEvent);
            }
            catch (Exception e)
            {
                logger.LogError("Unexpected exception queueing event: {Exception}", e.Message);
            }
        }

        public void AddFlushedEventSubscriber(EventHandler<DVCEventArgs> flushedEventsSubscriber)
        {
            eventQueue.AddFlushedEventsSubscriber(flushedEventsSubscriber);
        }

        public void RemoveFlushedEventsSubscriber(EventHandler<DVCEventArgs> flushedEventsSubscriber)
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

        public override IDVCApiClient GetApiClient()
        {
            throw new NotImplementedException();
        }

        /**
         * Rebuild the logging of the client and all subcomponents
         */
        public override void UpdateLogging(ILoggerFactory loggerFactory) {
            if (loggerFactory != null) {
                logger = loggerFactory.CreateLogger<DVCLocalClient>();

                if (configManager != null)
                {
                    configManager.UpdateLogging(loggerFactory);
                }

                if (eventQueue != null)
                {
                    eventQueue.UpdateLogging(loggerFactory);
                }
                
                if(localBucketing != null)
                {
                    localBucketing.UpdateLogging(loggerFactory);
                }
            }
        }
    }
}
