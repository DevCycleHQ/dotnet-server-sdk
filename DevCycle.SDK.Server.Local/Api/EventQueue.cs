using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DevCycle.SDK.Server.Common.Exception;
using DevCycle.SDK.Server.Common.Model;
using DevCycle.SDK.Server.Common.Model.Local;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RestSharp.Portable;


namespace DevCycle.SDK.Server.Local.Api
{
    internal class UserAndFeatureVars
    {
        public readonly DVCPopulatedUser User;
        private readonly Dictionary<string, string> featureVars;

        public UserAndFeatureVars(DVCPopulatedUser user, Dictionary<string, string> featureVars)
        {
            User = user;
            this.featureVars = featureVars;
        }

        private int FeatureVarsHashCode()
        {
            var sum = 0;
            foreach (var entry in featureVars)
            {
                sum += entry.Key.GetHashCode();
                sum += entry.Value.GetHashCode();
            }

            return sum;
        }

        public override int GetHashCode()
        {
            return User.GetHashCode() + FeatureVarsHashCode();
        }

        public override bool Equals(object obj)
        {
            return GetHashCode().Equals(obj?.GetHashCode());
        }
    }

    internal class AggregateEventQueues
    {
        private readonly Dictionary<
            UserAndFeatureVars,
            Dictionary<string, DVCRequestEvent>> eventQueueMap;
        
        public AggregateEventQueues()
        {
            eventQueueMap = new Dictionary<UserAndFeatureVars, Dictionary<string, DVCRequestEvent>>();
        }

        public void AddEvent(UserAndFeatureVars userFeatureVars, DVCRequestEvent requestEvent)
        {
            if (!eventQueueMap.ContainsKey(userFeatureVars))
            {
                eventQueueMap[userFeatureVars] = new Dictionary<string, DVCRequestEvent>();
            }

            var eventKey = GetEventMapKey(requestEvent);

            if (!eventQueueMap[userFeatureVars].ContainsKey(eventKey))
            {
                eventQueueMap[userFeatureVars][eventKey] = requestEvent;
            }
            else
            {
                eventQueueMap[userFeatureVars][eventKey].Value += 1;
            }
        }

        private string GetEventMapKey(DVCRequestEvent requestEvent)
        {
            return requestEvent.Type + requestEvent.Target;
        }

        public Dictionary<DVCPopulatedUser, UserEventsBatchRecord> GetEventBatches()
        {
            // regroup aggregate events into batches by unique user
            var userEventBatches = new Dictionary<DVCPopulatedUser, UserEventsBatchRecord>();
            
            foreach (var entries in eventQueueMap)
            {
                var user = entries.Key.User;
                if (!userEventBatches.ContainsKey(user))
                {
                    userEventBatches[user] = new UserEventsBatchRecord(user);
                }
                
                userEventBatches[user].Events.AddRange(entries.Value.Values.ToList());
            }
            
            return userEventBatches;
        }

        public void Clear()
        {
            eventQueueMap.Clear();
        }
    }

    internal class EventQueue
    {
        private readonly DVCOptions options;
        private readonly DVCEventsApiClient dvcEventsApiClient;
        
        private static readonly SemaphoreSlim EventQueueSemaphore = new(1,1);
        
        private readonly Mutex eventQueueMutex = new();
        private readonly Mutex aggregateEventQueueMutex = new();
        private readonly Mutex batchQueueMutex = new();
        
        private readonly Dictionary<DVCPopulatedUser, UserEventsBatchRecord> eventPayloadsToFlush;
        
        private readonly AggregateEventQueues aggregateEvents;

        private readonly List<BatchOfUserEventsBatch> batchQueue = new();

        private readonly ILogger logger;

        private CancellationTokenSource tokenSource = new CancellationTokenSource();
        private bool schedulerIsRunning;
        private event EventHandler<DVCEventArgs> FlushedEvents;

        // Internal parameterless constructor for testing with Moq
        internal EventQueue() : this("not-a-real-key", new DVCOptions(100, 100), new NullLoggerFactory())
        {
        }

        public EventQueue(string environmentKey, DVCOptions options, ILoggerFactory loggerFactory)
        {
            dvcEventsApiClient = new DVCEventsApiClient(environmentKey);
            this.options = options;
            
            eventPayloadsToFlush = new Dictionary<DVCPopulatedUser, UserEventsBatchRecord>();
            aggregateEvents = new AggregateEventQueues();

            logger = loggerFactory.CreateLogger<EventQueue>();
        }
        
        public void AddFlushedEventsSubscriber(EventHandler<DVCEventArgs> flushedEventsSubscriber)
        {
            FlushedEvents += flushedEventsSubscriber;
        }

        public void RemoveFlushedEventsSubscriber(EventHandler<DVCEventArgs> flushedEventsSubscriber)
        {
            FlushedEvents -= flushedEventsSubscriber;
        }
        
        public virtual async Task FlushEvents()
        {
            // cancel pending queued FlushEvents
            tokenSource.Cancel();

            await EventQueueSemaphore.WaitAsync(0);
            var eventArgs = new DVCEventArgs();
            try
            {
                eventQueueMutex.WaitOne();
                aggregateEventQueueMutex.WaitOne();
                batchQueueMutex.WaitOne();

                var userEventBatch = CombineUsersEventsToFlush();

                if (userEventBatch.Count != 0)
                {
                    batchQueue.Add(new BatchOfUserEventsBatch(userEventBatch.Values.ToList()));
                    eventPayloadsToFlush.Clear();
                    aggregateEvents.Clear();
                }
                
                eventQueueMutex.ReleaseMutex();
                aggregateEventQueueMutex.ReleaseMutex();

                if (batchQueue.Count == 0)
                {
                    eventArgs.Success = true;
                    OnFlushedEvents(eventArgs);
                }

                var eventCount = userEventBatch.Sum(u => u.Value.Events.Count);

                logger.LogInformation("DVC Flush {EventCount} Events, for {UserEventBatch} Users", eventCount, userEventBatch.Count);
                
                IRestResponse response = null;

                var successfulRequests = new List<BatchOfUserEventsBatch>();

                try
                {
                    foreach (var batch in batchQueue)
                    {
                        response = await dvcEventsApiClient.PublishEvents(batch);

                        if (response.StatusCode != HttpStatusCode.Created)
                        {
                            throw new System.Exception(
                                $"Error publishing events, status {response.StatusCode}, body: {batch}");
                        }
                        
                        successfulRequests.Add(batch);
                    }

                    batchQueue.Clear();

                    batchQueue.AddRange(batchQueue.Except(successfulRequests));
                    
                    logger.LogDebug("DVC Flushed {EventCount} Events, for {UserEventBatch} Users", eventCount,
                        userEventBatch.Count);
                    eventArgs.Success = true;
                    OnFlushedEvents(eventArgs);
                }
                catch (System.Exception e)
                {
                    logger.LogError(
                        "DVC Error Flushing Events response message: {Exception}, response data: {Response}", e.Message,
                        response.Content);

                    ScheduleFlushWithDelay(true);
                    var dvcException = new DVCException(response.StatusCode, new ErrorResponse(e.Message));
                    eventArgs.Success = false;
                    eventArgs.Error = dvcException;
                    OnFlushedEvents(eventArgs);
                }
                finally
                {
                    batchQueueMutex.ReleaseMutex();
                }
            }
            finally
            {
                EventQueueSemaphore.Release();
            }
        }

        public virtual void QueueEvent(DVCPopulatedUser user, Event @event, BucketedUserConfig config)
        {
            eventQueueMutex.WaitOne();
            if (!eventPayloadsToFlush.ContainsKey(user))
            {
                eventPayloadsToFlush.Add(user, new UserEventsBatchRecord(user));
            }
            
            var userAndEvents = eventPayloadsToFlush[user];

            userAndEvents.Events.Add(new DVCRequestEvent(@event, user.UserId, config.FeatureVariationMap));
            
            eventQueueMutex.ReleaseMutex();
            
            logger.LogInformation("{Event} queued successfully", @event);
            
            ScheduleFlushWithDelay();
        }

        /**
         * Queue Event that can be aggregated together, where multiple calls are aggregated
         * by incrementing the 'value' field.
         */
        public virtual void QueueAggregateEvent(DVCPopulatedUser user, Event @event, BucketedUserConfig config)
        {
            if (string.IsNullOrEmpty(user.UserId))
            {
                throw new ArgumentException("UserId must be set");
            }
            if (string.IsNullOrEmpty(@event.Target))
            {
                throw new ArgumentException("Target must be set");
            }
            if (@event.Type == string.Empty)
            {
                throw new ArgumentException("Type must be set");
            }

            var eventCopy = @event.Clone();
            eventCopy.Date = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            eventCopy.Value = 1;

            var requestEvent = new DVCRequestEvent(
                eventCopy, 
                user.UserId, 
                config == null ? new Dictionary<string, string>() : config.FeatureVariationMap
            );
            
            var userAndFeatureVars = new UserAndFeatureVars(user, requestEvent.FeatureVars);

            aggregateEventQueueMutex.WaitOne();
            aggregateEvents.AddEvent(userAndFeatureVars, requestEvent);
            aggregateEventQueueMutex.ReleaseMutex();
            ScheduleFlushWithDelay();
        }

        private Dictionary<DVCPopulatedUser, UserEventsBatchRecord> CombineUsersEventsToFlush()
        {
            var userEventsBatchRecords = aggregateEvents.GetEventBatches();

            foreach (var eventPayload in eventPayloadsToFlush)
            {
                var user = eventPayload.Key;
                var userEventsRecord = eventPayload.Value;

                if (userEventsBatchRecords.ContainsKey(user))
                {
                    userEventsBatchRecords[user].Events.AddRange(userEventsRecord.Events);
                }
                else
                {
                    userEventsBatchRecords.Add(user, userEventsRecord);
                }
            }

            return userEventsBatchRecords;
        }

        private IEnumerable<DVCRequestEvent> EventsFromAggregateEvents(Dictionary<string, Dictionary<string, DVCRequestEvent>> aggUserEventsRecord)
        {
            return (from eventType in aggUserEventsRecord 
                from eventTarget in eventType.Value select eventTarget.Value).ToList();
        }

        private void ScheduleFlushWithDelay(bool queueRequest = false)
        {
            if (schedulerIsRunning && !queueRequest) return;

            schedulerIsRunning = true;
            tokenSource = new CancellationTokenSource();

            Task.Run(async delegate
            {
                if (tokenSource.IsCancellationRequested)
                {
                    schedulerIsRunning = false;
                    tokenSource.Token.ThrowIfCancellationRequested();
                }
                
                await Task.Delay(options.ConfigPollingIntervalMs);
                if (tokenSource.IsCancellationRequested)
                {
                    schedulerIsRunning = false;
                    tokenSource.Token.ThrowIfCancellationRequested();
                }

                await FlushEvents();
                schedulerIsRunning = false;
            }, tokenSource.Token);
        }
        
        private void OnFlushedEvents(DVCEventArgs e)
        {
            FlushedEvents?.Invoke(this, e);
        }
    }
}