using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using DevCycle.SDK.Server.Common.API;
using DevCycle.SDK.Server.Common.Model;
using DevCycle.SDK.Server.Common.Model.Local;
using DevCycle.SDK.Server.Local.ConfigManager;
using Microsoft.Extensions.Logging;

namespace DevCycle.SDK.Server.Local.Api
{
    public class DVCLocalClientBuilder : DVCClientBuilder
    {
        private EnvironmentConfigManager configManager;
        private ILocalBucketing localBucketing;
       
        
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
        
        public override IDVCClient Build()
        {
            localBucketing ??= new LocalBucketing();

            options ??= new DVCOptions();

            loggerFactory ??= LoggerFactory.Create(builder => builder.AddConsole());

            configManager ??= new EnvironmentConfigManager(environmentKey, options, loggerFactory, localBucketing, initialized);

            return new DVCLocalClient(environmentKey, options, loggerFactory, configManager, localBucketing, proxy);
        }
    }

    public sealed class DVCLocalClient : DVCBaseClient
    {
        private readonly string environmentKey;
        private readonly EnvironmentConfigManager configManager;
        private readonly EventQueue eventQueue;
        private readonly ILocalBucketing localBucketing;
        private readonly ILogger logger;

        internal DVCLocalClient(string environmentKey, DVCOptions dvcOptions, ILoggerFactory loggerFactory,
            EnvironmentConfigManager configManager, ILocalBucketing localBucketing, IWebProxy proxy)
        {
            eventQueue = new EventQueue(environmentKey, dvcOptions, loggerFactory, proxy);
            this.environmentKey = environmentKey;
            this.configManager = configManager;
            this.localBucketing = localBucketing;
            logger = loggerFactory.CreateLogger<DVCLocalClient>();

            Task.Run(async delegate
            {
                await configManager.InitializeConfigAsync();
            });
        }

        public Variable<T> Variable<T>(User user, string key, T defaultValue)
        {
            var requestUser = new DVCPopulatedUser(user);

            if (!configManager.Initialized)
            {
                logger.LogWarning("Variable called before DVCClient has initialized, returning default value");

                eventQueue.QueueAggregateEvent(
                    requestUser, 
                    new Event(type: EventTypes.variableDefaulted, target: key), 
                    null
                );
                
                return Common.Model.Local.Variable<T>.InitializeFromVariable(null, key, defaultValue);
            }

            var platformData = new PlatformData(requestUser);

            BucketedUserConfig config = null;

            try
            {
                localBucketing.SetPlatformData(platformData.ToJson());
                config = localBucketing.GenerateBucketedConfig(environmentKey, requestUser.ToJson());
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
                    existingVariable = config.Variables.Get<T>(key);
                } 
                catch (InvalidCastException)
                {
                    logger.LogWarning("Type of Variable does not match DevCycle configuration. Using default value.");
                }
            }

            var variable =  Common.Model.Local.Variable<T>.InitializeFromVariable(existingVariable, key, defaultValue);

            var @event = new Event(type: variable.IsDefaulted
                    ? EventTypes.variableDefaulted
                    : EventTypes.variableEvaluated,
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
            var platformData = new PlatformData(requestUser);

            try
            {
                localBucketing.SetPlatformData(platformData.ToJson());
                var config = localBucketing.GenerateBucketedConfig(environmentKey, requestUser.ToJson());
                return config.Features;
            }
            catch (Exception e)
            {
                logger.LogError("Unexpected exception retrieving features from the config {Exception}", e.Message);
                return new Dictionary<string, Feature>();
            }
        }

        public VariableCollection AllVariables(User user)
        {
            if (!configManager.Initialized)
            {
                logger.LogWarning("AllVariables called before DVCClient has initialized");
                return new VariableCollection(new Dictionary<string, Variable<object>>());
            }

            var requestUser = new DVCPopulatedUser(user);
            var platformData = new PlatformData(requestUser);

            try
            {
                localBucketing.SetPlatformData(platformData.ToJson());
                var config = localBucketing.GenerateBucketedConfig(environmentKey, requestUser.ToJson());
                return config.Variables;
            }
            catch (Exception e)
            {
                logger.LogError("Unexpected exception retrieving variables from the config: {Exception}", e.Message);
                return new VariableCollection(new Dictionary<string, Variable<object>>());
            }
        }

        public void Track(User user, Event userEvent)
        {
            BucketedUserConfig config = null;
            var requestUser = new DVCPopulatedUser(user);
            var platformData = new PlatformData(requestUser);

            if (!configManager.Initialized)
            {
                logger.LogWarning("Track called before DVCClient has initialized");
            }
            else
            {
                localBucketing.SetPlatformData(platformData.ToJson());
                config = localBucketing.GenerateBucketedConfig(environmentKey, requestUser.ToJson());
            }


            try
            {
                eventQueue.QueueEvent(requestUser, userEvent, config);
            }
            catch (Exception e)
            {
                logger.LogError("Unexpected exception generating bucketed config: {Exception}", e.Message);
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
            configManager.Dispose();
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