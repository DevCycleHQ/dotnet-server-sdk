using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using DevCycle.SDK.Server.Common.API;
using DevCycle.SDK.Server.Common.Model;
using DevCycle.SDK.Server.Common.Model.Local;
using DevCycle.SDK.Server.Local.ConfigManager;
using Microsoft.Extensions.Logging;
using RestSharp;

namespace DevCycle.SDK.Server.Local.Api
{
    public class DVCLocalClientBuilder : DVCClientBuilder<DVCLocalClient, DVCLocalOptions, DVCLocalClientBuilder>
    {
        private EnvironmentConfigManager configManager;
        private ILocalBucketing localBucketing;
        
        protected override DVCLocalClientBuilder BuilderInstance => this;

        public DVCLocalClientBuilder SetConfigManager(EnvironmentConfigManager environmentConfigManager)
        {
            configManager = environmentConfigManager;
            return this;
        }

        public DVCLocalClientBuilder SetLocalBucketing(ILocalBucketing localBucketingWrapper)
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
            localBucketing ??= new LocalBucketing();

            options ??= new DVCLocalOptions();

            loggerFactory ??= LoggerFactory.Create(builder => builder.AddConsole());

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
        private readonly ILocalBucketing localBucketing;
        private readonly ILogger logger;
        private readonly Timer timer;
        private bool closing;

        internal DVCLocalClient(
            string sdkKey, 
            DVCLocalOptions dvcLocalOptions, 
            ILoggerFactory loggerFactory,
            EnvironmentConfigManager configManager, 
            ILocalBucketing localBucketing,
            DVCRestClientOptions restClientOptions = null
        )  {
            ValidateSDKKey(sdkKey);
            this.sdkKey = sdkKey;
            this.configManager = configManager;
            this.localBucketing = localBucketing;
            logger = loggerFactory.CreateLogger<DVCLocalClient>();
            eventQueue = new EventQueue(sdkKey, dvcLocalOptions, loggerFactory, localBucketing, restClientOptions);

            Task.Run(async delegate { await this.configManager.InitializeConfigAsync(); });
            var platformData = new PlatformData();
            localBucketing.SetPlatformData(platformData.ToJson());

            timer = new Timer(dvcLocalOptions.EventFlushIntervalMs);
            timer.Elapsed += OnTimedEvent;
            timer.AutoReset = true;
            timer.Enabled = true;
        }
        
        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            eventQueue?.ScheduleFlush();
        }

        public Variable<T> Variable<T>(User user, string key, T defaultValue)
        {
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


            BucketedUserConfig config = null;

            try
            {
                config = localBucketing.GenerateBucketedConfig(sdkKey, requestUser.ToJson());
            }
            catch (Exception e)
            {
                logger.LogError("Unexpected exception generating bucketed config: {Exception}", e.Message);
            }

            Variable<T> existingVariable = null;

            if (config?.Variables != null && config.Variables.ContainsKey(key))
            {
                try
                {
                    ReadOnlyVariable<object> readOnlyVariable = config.Variables[key];
                    existingVariable = new Variable<T>(readOnlyVariable, defaultValue);                
                }
                catch (InvalidCastException)
                {
                    logger.LogWarning("Type of Variable does not match DevCycle configuration. Using default value");
                }
            }

            var variable = Common.Model.Local.Variable<T>.InitializeFromVariable(existingVariable, key, defaultValue);

            var @event = new Event(type: variable.IsDefaulted
                    ? EventTypes.aggVariableDefaulted
                    : EventTypes.aggVariableEvaluated,
                target: variable.Key);

            eventQueue.QueueAggregateEvent(requestUser, @event, config);

            return variable;
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
    }
}
