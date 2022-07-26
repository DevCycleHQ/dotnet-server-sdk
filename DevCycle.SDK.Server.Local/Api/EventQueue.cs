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
using RestSharp;


namespace DevCycle.SDK.Server.Local.Api
{
    internal class EventQueue
    {
        private readonly DVCLocalOptions localOptions;
        private readonly DVCEventsApiClient dvcEventsApiClient;

        private static readonly SemaphoreSlim EventQueueSemaphore = new(1, 1);

        private readonly SemaphoreSlim eventQueueMutex = new(1, 1);
        private readonly SemaphoreSlim aggregateEventQueueMutex = new(1, 1);
        private readonly SemaphoreSlim batchQueueMutex = new(1, 1);

        private readonly Dictionary<DVCPopulatedUser, UserEventsBatchRecord> eventPayloadsToFlush;

        private readonly AggregateEventQueues aggregateEvents;

        private readonly List<BatchOfUserEventsBatch> batchQueue = new();

        private readonly ILogger logger;

        private CancellationTokenSource tokenSource = new();
        private bool schedulerIsRunning;
        private event EventHandler<DVCEventArgs> FlushedEvents;

        // Internal parameterless constructor for testing with Moq
        internal EventQueue() : this("not-a-real-key", new DVCLocalOptions(100, 100), new NullLoggerFactory())
        {
        }

        public EventQueue(string environmentKey, DVCLocalOptions localOptions, ILoggerFactory loggerFactory,
            RestClientOptions restClientOptions = null)
        {
            dvcEventsApiClient = new DVCEventsApiClient(environmentKey, localOptions, restClientOptions);
            this.localOptions = localOptions;
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
                await eventQueueMutex.WaitAsync();
                await aggregateEventQueueMutex.WaitAsync();
                await batchQueueMutex.WaitAsync();

                var userEventBatch = CombineUsersEventsToFlush();

                if (userEventBatch.Count != 0)
                {
                    batchQueue.Add(new BatchOfUserEventsBatch(userEventBatch.Values.ToList()));
                    eventPayloadsToFlush.Clear();
                    aggregateEvents.Clear();
                }

                eventQueueMutex.Release();
                aggregateEventQueueMutex.Release();

                if (batchQueue.Count == 0)
                {
                    eventArgs.Success = true;
                    OnFlushedEvents(eventArgs);
                }

                var eventCount = userEventBatch.Sum(u => u.Value.Events.Count);

                logger.LogInformation("DVC Flush {EventCount} Events, for {UserEventBatch} Users", eventCount,
                    userEventBatch.Count);

                RestResponse response = null;

                var completedRequests = new List<BatchOfUserEventsBatch>();

                try
                {
                    foreach (var batch in batchQueue)
                    {
                        response = await dvcEventsApiClient.PublishEvents(batch);

                        if (response.StatusCode == HttpStatusCode.Created) continue;
                        var error = new DVCException(response.StatusCode,
                            new ErrorResponse(response.Content ?? "Something went wrong flushing events"));

                        if (!error.IsRetryable())
                        {
                            // Add non-retryable payloads to list of "completed" so that they are removed from queue
                            completedRequests.Add(batch);
                        }

                        throw error;
                    }

                    batchQueue.Clear();

                    logger.LogDebug("DVC Flushed {EventCount} Events, for {UserEventBatch} Users", eventCount,
                        userEventBatch.Count);
                    eventArgs.Success = true;
                    OnFlushedEvents(eventArgs);
                }
                catch (DVCException e)
                {
                    if (e.IsRetryable())
                    {
                        logger.LogError(
                            "Error publishing events, retrying, status {status}, body: {response}", e.HttpStatusCode,
                            response?.Content);
                        ScheduleFlushWithDelay(true);
                    }
                    else
                    {
                        logger.LogError(
                            "DVC Events were invalid and have been dropped, status {status}, body: {response}",
                            e.HttpStatusCode,
                            response?.Content);
                    }

                    eventArgs.Success = false;
                    eventArgs.Error = e;
                    OnFlushedEvents(eventArgs);
                }
                catch (Exception e)
                {
                    logger.LogError(
                        "Something went wrong flushing events, retrying, {message}", e.Message);
                    ScheduleFlushWithDelay(true);
                    eventArgs.Success = false;
                    eventArgs.Error =
                        new DVCException(HttpStatusCode.InternalServerError, new ErrorResponse(e.Message));
                    OnFlushedEvents(eventArgs);
                }
                finally
                {
                    var retryableRequests = batchQueue.Except(completedRequests);
                    var retryable = retryableRequests.ToList();
                    batchQueue.Clear();

                    batchQueue.AddRange(retryable);
                    batchQueueMutex.Release();
                }
            }
            finally
            {
                EventQueueSemaphore.Release();
            }
        }

        public virtual void QueueEvent(DVCPopulatedUser user, Event @event, BucketedUserConfig config, bool throwOnQueueMax = false)
        {
            if ((localOptions.DisableCustomEvents && @event.Type.Equals("customEvent")) ||
                localOptions.DisableAutomaticEvents && !@event.Type.Equals("customEvent"))
                return;

            if (IsOverMaxQueue())
            {
                logger.LogWarning(
                    "{Event} failed to be queued; events in queue exceed {Max}. Triggering a forced flush", @event,
                    localOptions.MaxEventsInQueue);
                ScheduleFlushWithDelay();
                if (throwOnQueueMax)
                    throw new DVCException(
                        new ErrorResponse("Failed to queue an event. Events in queue exceeded the max"));
                logger.Log(LogLevel.Error, "Failed to queue an event. Events in queue exceeded the max");
                return;
            }

            eventQueueMutex.Wait();
            if (!eventPayloadsToFlush.ContainsKey(user))
            {
                eventPayloadsToFlush.Add(user, new UserEventsBatchRecord(user));
            }

            var userAndEvents = eventPayloadsToFlush[user];

            var featureVars = config?.FeatureVariationMap ?? new Dictionary<string, string>();

            userAndEvents.Events.Add(new DVCRequestEvent(@event, user.UserId, featureVars));

            eventQueueMutex.Release();

            logger.LogInformation("{Event} queued successfully", @event);

            ScheduleFlushWithDelay();
        }

        /**
         * Queue Event that can be aggregated together, where multiple calls are aggregated
         * by incrementing the 'value' field.
         */
        public virtual void QueueAggregateEvent(DVCPopulatedUser user, Event @event, BucketedUserConfig config, bool throwOnQueueMax = false)
        {
            if ((localOptions.DisableCustomEvents && @event.Type.Equals("customEvent")) ||
                localOptions.DisableAutomaticEvents && !@event.Type.Equals("customEvent"))
                return;

            if (IsOverMaxQueue())
            {
                logger.LogWarning("{Event} failed to be queued; events in queue exceed {Max}", @event,
                    localOptions.MaxEventsInQueue);
                ScheduleFlushWithDelay();
                if (throwOnQueueMax)
                    throw new DVCException(
                        new ErrorResponse("Failed to queue an event. Events in queue exceeded the max"));
                logger.Log(LogLevel.Error, "Failed to queue an event. Events in queue exceeded the max");
                return;                return;
            }

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

            aggregateEventQueueMutex.Wait();
            aggregateEvents.AddEvent(userAndFeatureVars, requestEvent);
            aggregateEventQueueMutex.Release();
            ScheduleFlushWithDelay();
        }

        private Dictionary<DVCPopulatedUser, UserEventsBatchRecord> CombineUsersEventsToFlush()
        {
            var userEventsBatchRecords = aggregateEvents.GetEventBatches();

            foreach (var (user, userEventsRecord) in eventPayloadsToFlush)
            {
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

        private IEnumerable<DVCRequestEvent> EventsFromAggregateEvents(
            Dictionary<string, Dictionary<string, DVCRequestEvent>> aggUserEventsRecord)
        {
            return (from eventType in aggUserEventsRecord
                from eventTarget in eventType.Value
                select eventTarget.Value).ToList();
        }

        private bool IsOverMaxQueue()
        {
            return CombineUsersEventsToFlush().Sum(u => u.Value.Events.Count) + batchQueue.Count >=
                   localOptions.MaxEventsInQueue;
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

                await Task.Delay(localOptions.ConfigPollingIntervalMs);
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