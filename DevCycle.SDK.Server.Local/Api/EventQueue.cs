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
    internal class EventQueue
    {
        private readonly DVCOptions options;
        private readonly DVCEventsApiClient dvcEventsApiClient;
        
        private static readonly SemaphoreSlim EventQueueSemaphore = new(1,1);
        
        private readonly Mutex eventQueueMutex = new();
        private readonly Mutex aggregateEventQueueMutex = new();

        private readonly Dictionary<string, UserEventsBatchRecord> eventPayloadsToFlush;
        
        private readonly Dictionary<string, DVCPopulatedUser> userForAggregation;
        private readonly Dictionary<string, Dictionary<string, Dictionary<string, DVCRequestEvent>>> aggregateEvents;

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
            
            eventPayloadsToFlush = new Dictionary<string, UserEventsBatchRecord>();
            userForAggregation = new Dictionary<string, DVCPopulatedUser>();
            aggregateEvents = new Dictionary<string, Dictionary<string, Dictionary<string, DVCRequestEvent>>>();

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
                var userEventBatch = CombineUsersEventsToFlush();
                if (userEventBatch.Count == 0)
                {
                    eventArgs.Success = true;
                    OnFlushedEvents(eventArgs);
                }

                eventQueueMutex.WaitOne();
                var localEventPayloadsToFlush = eventPayloadsToFlush.ToDictionary(entry => entry.Key, 
                    entry => entry.Value);
                eventPayloadsToFlush.Clear();
                eventQueueMutex.ReleaseMutex();

                aggregateEventQueueMutex.WaitOne();
                var localUserForAggregation = userForAggregation.ToDictionary(entry => entry.Key,
                    entry => entry.Value);
                var localAggregateEvents = aggregateEvents.ToDictionary(entry => entry.Key,
                    entry => entry.Value);
                userForAggregation.Clear();
                aggregateEvents.Clear();
                aggregateEventQueueMutex.ReleaseMutex();

                var eventCount = userEventBatch.Sum(u => userEventBatch.Values.Count);

                logger.LogInformation("DVC Flush {EventCount} Events, for {UserEventBatch} Users", eventCount, userEventBatch.Count);

                var userEvents = userEventBatch.Select(u => u.Value).ToList();

                IRestResponse response = null;

                try
                {
                    BatchOfUserEventsBatch batch = new BatchOfUserEventsBatch(userEvents);
                    response = await dvcEventsApiClient.PublishEvents(batch);

                    if (response.StatusCode != HttpStatusCode.Created)
                    {
                        throw new System.Exception(
                            $"Error publishing events, status {response.StatusCode}, body: {userEvents}");
                    }
                    
                    logger.LogDebug("DVC Flushed {EventCount} Events, for {UserEventBatch} Users", eventCount, userEventBatch.Count);
                    eventArgs.Success = true;
                    OnFlushedEvents(eventArgs);
                }
                catch (System.Exception e)
                {
                    logger.LogError("DVC Error Flushing Events response message: {Exception}, response data: {Response}", e.Message, response.Content);
                    
                    RequeueUserEvents(localEventPayloadsToFlush);
                    RequeueAggregateEvents(localUserForAggregation, localAggregateEvents);
                    ScheduleFlushWithDelay(true);
                    var dvcException = new DVCException(response.StatusCode, new ErrorResponse(e.Message));
                    eventArgs.Success = false;
                    eventArgs.Error = dvcException;
                    OnFlushedEvents(eventArgs);
                }
            }
            finally
            {
                EventQueueSemaphore.Release();
            }
        }

        public virtual void QueueEvent(DVCPopulatedUser user, Event @event, BucketedUserConfig config)
        {
            if (!eventPayloadsToFlush.ContainsKey(user.UserId))
            {
                eventPayloadsToFlush.Add(user.UserId, new UserEventsBatchRecord(user));
            }
            
            var userAndEvents = eventPayloadsToFlush[user.UserId];
            userAndEvents.User = user;

            userAndEvents.Events.Add(new DVCRequestEvent(@event, user.UserId, config.FeatureVariationMap));
            
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

            if (!userForAggregation.ContainsKey(user.UserId))
            {
                userForAggregation.Add(user.UserId, user);   
            }
            else
            {
                userForAggregation[user.UserId] = user;
            }
            
            var requestEvent = new DVCRequestEvent(
                eventCopy, 
                user.UserId, 
                config == null ? new Dictionary<string, string>() : config.FeatureVariationMap
            );
            
            AddAggregateEvent(requestEvent, user);

            ScheduleFlushWithDelay();
        }

        private void AddAggregateEvent(DVCRequestEvent requestEvent, DVCPopulatedUser user)
        {
            var aggregateUser = aggregateEvents.ContainsKey(user.UserId)
                ? aggregateEvents[user.UserId]
                : new Dictionary<string, Dictionary<string, DVCRequestEvent>>();

            var aggregateEventType = aggregateUser.ContainsKey(requestEvent.Type)
                ? aggregateUser[requestEvent.Type]
                : AddAggregateEventType(user, aggregateUser, requestEvent);

            if (aggregateEventType.ContainsKey(requestEvent.Target))
            {
                aggregateEventType[requestEvent.Target].Value += Decimal.One;
            }
            else
            {
                aggregateEventType.Add(requestEvent.Target, requestEvent);
            }
        }

        private Dictionary<string, DVCRequestEvent> AddAggregateEventType(DVCPopulatedUser user,
            Dictionary<string, Dictionary<string, DVCRequestEvent>> aggregateUser,
            DVCRequestEvent requestEvent)
        {
            aggregateUser.Add(requestEvent.Type, new Dictionary<string, DVCRequestEvent>());
            aggregateUser[requestEvent.Type].Add(requestEvent.Target, requestEvent);
            aggregateEvents.Add(user.UserId, aggregateUser);

            return aggregateUser[requestEvent.Type];
        }

        private Dictionary<string, UserEventsBatchRecord> CombineUsersEventsToFlush()
        {
            var userEventsBatchRecords = new Dictionary<string, UserEventsBatchRecord>();

            foreach (var eventPayload in eventPayloadsToFlush)
            {
                var userId = eventPayload.Key;
                var userEventsRecord = eventPayload.Value;

                if (userEventsBatchRecords.ContainsKey(userId))
                {
                    userEventsBatchRecords[userId].Events.AddRange(userEventsRecord.Events);
                }
                else
                {
                    userEventsBatchRecords.Add(userId, userEventsRecord);
                }
            }

            foreach (var userEvent in userForAggregation)
            {
                var userId = userEvent.Key;
                var user = userEvent.Value;
                var aggUserEventsRecord = aggregateEvents[userId];
                var events = EventsFromAggregateEvents(aggUserEventsRecord);

                if (!userEventsBatchRecords.ContainsKey(userId))
                {
                    var userEventsRecord = new UserEventsBatchRecord(user);
                    userEventsBatchRecords.Add(userId, userEventsRecord);
                }
                userEventsBatchRecords[userId].Events.AddRange(events);
            }

            return userEventsBatchRecords;
        }

        private IEnumerable<DVCRequestEvent> EventsFromAggregateEvents(Dictionary<string, Dictionary<string, DVCRequestEvent>> aggUserEventsRecord)
        {
            return (from eventType in aggUserEventsRecord 
                from eventTarget in eventType.Value select eventTarget.Value).ToList();
        }
        
        private void RequeueAggregateEvents(Dictionary<string,DVCPopulatedUser> localUserForAggregation,
            Dictionary<string,Dictionary<string,Dictionary<string,DVCRequestEvent>>> localAggregateEvents)
        {
            foreach (var localUser in localUserForAggregation)
            {
                if (userForAggregation.ContainsKey(localUser.Key))
                {
                    var aggregateEvent = localAggregateEvents[localUser.Key];

                    foreach (var @event in EventsFromAggregateEvents(aggregateEvent))
                    {
                        AddAggregateEvent(@event, localUser.Value);
                    } 
                }
            }
        }

        private void RequeueUserEvents(Dictionary<string,UserEventsBatchRecord> localEventPayloadsToFlush)
        {
            foreach (var userEventsBatchRecord in localEventPayloadsToFlush)
            {
                if (eventPayloadsToFlush.ContainsKey(userEventsBatchRecord.Key))
                {
                    eventPayloadsToFlush[userEventsBatchRecord.Key].Events.AddRange(userEventsBatchRecord.Value.Events);
                }
                else
                {
                    eventPayloadsToFlush.Add(userEventsBatchRecord.Key, userEventsBatchRecord.Value);
                }
            }
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