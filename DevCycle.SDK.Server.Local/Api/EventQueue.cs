using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DevCycle.SDK.Server.Common.API;
using DevCycle.SDK.Server.Common.Exception;
using DevCycle.SDK.Server.Common.Model;
using DevCycle.SDK.Server.Common.Model.Local;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;

namespace DevCycle.SDK.Server.Local.Api
{
    internal class EventQueue
    {
        private readonly DevCycleLocalOptions localOptions;
        private readonly DevCycleEventsApiClient devCycleEventsApiClient;
        private readonly LocalBucketing localBucketing;
        private readonly string sdkKey;
        private bool closing;

        private readonly ILogger logger;

        private CancellationTokenSource tokenSource = new();
        private bool schedulerIsRunning;
        private event EventHandler<DevCycleEventArgs> FlushedEvents;

        public EventQueue(
            string sdkKey,
            DevCycleLocalOptions localOptions,
            ILoggerFactory loggerFactory,
            LocalBucketing localBucketing,
            DevCycleRestClientOptions restClientOptions = null
        )
        {
            devCycleEventsApiClient = new DevCycleEventsApiClient(sdkKey, localOptions, restClientOptions);
            this.sdkKey = sdkKey;
            this.localOptions = localOptions;
            this.localBucketing = localBucketing;
            this.localBucketing.InitEventQueue(sdkKey, JsonConvert.SerializeObject(localOptions));

            logger = loggerFactory.CreateLogger<EventQueue>();
        }

        public void AddFlushedEventsSubscriber(EventHandler<DevCycleEventArgs> flushedEventsSubscriber)
        {
            FlushedEvents += flushedEventsSubscriber;
        }

        public void RemoveFlushedEventsSubscriber(EventHandler<DevCycleEventArgs> flushedEventsSubscriber)
        {
            FlushedEvents -= flushedEventsSubscriber;
        }

        private async Task<Tuple<FlushPayload, RestResponse>> GetPayloadResult(FlushPayload flushPayload)
        {
            return new Tuple<FlushPayload, RestResponse>(flushPayload,
                await devCycleEventsApiClient.PublishEvents(flushPayload.Records));
        }

        public virtual async Task FlushEvents()
        {
            localBucketing.StartFlush();
            var flushPayloads = GetPayloads();
            var flushResultEvent = new DevCycleEventArgs
            {
                Success = true
            };

            if (flushPayloads.Count == 0)
            {
                OnFlushedEvents(flushResultEvent);
                localBucketing.EndFlush();
                return;
            }

            logger.LogDebug($"Flush Payloads: {flushPayloads}");

            Func<int, FlushPayload, int> reducer = (val, batches) => val + batches.EventCount;
            var eventCount = flushPayloads.Aggregate(0, reducer);
            logger.LogDebug($"DevCycle Flush {eventCount} Events, for {flushPayloads.Count} Users");

            var requestTasks = flushPayloads.Select(GetPayloadResult).ToList();
            await Task.WhenAll(requestTasks);
            var results = requestTasks.Select(task => task.Result);
            foreach (var (flushPayload, res) in results)
            {
                try
                {
                    if (res.StatusCode != HttpStatusCode.Created)
                    {
                        logger.LogError($"Error publishing events, status: ${res.StatusCode}, body: ${res.Content}");
                        localBucketing.OnPayloadFailure(sdkKey, flushPayload.PayloadID, (int)res.StatusCode >= 500);
                        flushResultEvent.Success = false;
                        flushResultEvent.Errors.Add(new DevCycleException(res.StatusCode,
                            new ErrorResponse(res.ErrorMessage ?? "")));
                    }
                    else
                    {
                        logger.LogDebug(
                            $"DevCycle Flushed ${eventCount} Events, for ${flushPayload.Records.Count} Users");
                        localBucketing.OnPayloadSuccess(sdkKey, flushPayload.PayloadID);
                    }
                }
                catch (DevCycleException ex)
                {
                    logger.LogError($"DevCycle Error Flushing Events response message: ${ex.Message}");
                    localBucketing.OnPayloadFailure(sdkKey, flushPayload.PayloadID, true);
                    flushResultEvent.Success = false;
                    flushResultEvent.Errors.Add(ex);
                }
            }

            OnFlushedEvents(flushResultEvent);
            localBucketing.EndFlush();
        }

        private List<FlushPayload> GetPayloads()
        {
            List<FlushPayload> flushPayloads;
            try
            {
                flushPayloads = localBucketing.FlushEventQueue(sdkKey);
            }
            catch (Exception ex)
            {
                logger.LogError($"DevCycle Error Flushing Events: ${ex.Message}");
                throw;
            }

            return flushPayloads;
        }

        public virtual void QueueEvent(DevCyclePopulatedUser user, DevCycleEvent @event, bool throwOnQueueMax = false)
        {
            if (closing)
            {
                return;
            }

            if (user is null)
            {
                throw new Exception("User can't be null");
            }

            if (CheckEventQueueSize())
            {
                logger.LogWarning(
                    "{Event} failed to be queued; events in queue exceed {Max}. Triggering a forced flush", @event,
                    localOptions.MaxEventsInQueue);
                if (throwOnQueueMax)
                    throw new DevCycleException(
                        new ErrorResponse("Failed to queue an event. Events in queue exceeded the max"));
                logger.Log(LogLevel.Error, "Failed to queue an event. Events in queue exceeded the max");
                return;
            }

            localBucketing.QueueEvent(
                sdkKey,
                JsonConvert.SerializeObject(user),
                JsonConvert.SerializeObject(@event)
            );
            logger.LogDebug("{Event} queued successfully", @event);
        }

        public virtual void QueueSDKConfigEvent(RestRequest request, RestResponse response)
        {
            var popU = new DevCyclePopulatedUser(
                new DevCycleUser(userId: $"{localBucketing.ClientUUID}@{Dns.GetHostName()}"));
            QueueEvent(popU, new DevCycleEvent(
                type: EventTypes.sdkConfig,
                target: request.Resource,
                value: -1,
                metaData: new Dictionary<string, object>
                {
                    { "clientUUID", localBucketing.ClientUUID },
                    {
                        "reqEtag",
                        request.Parameters.GetParameters<HeaderParameter>().FirstOrDefault(p => p.Name == "If-None-Match")
                    },
                    {
                        "reqLastModified",
                        request.Parameters.GetParameters<HeaderParameter>().FirstOrDefault(p => p.Name == "If-Modified-Since")
                    },
                    {
                        "resEtag", response.Headers?.FirstOrDefault(p => p.Name?.ToLower() == "etag")
                    },
                    {
                        "resLastModified", response.ContentHeaders?.FirstOrDefault(p => p.Name?.ToLower() == "last-modified")
                    },
                    {
                        "resRayId", response.Headers?.FirstOrDefault(p => p.Name?.ToLower() == "cf-ray")
                    },
                    {
                        "resStatus", response.StatusCode
                    },
                    {
                        "errMsg",
                        response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.NotModified
                            ? response.StatusDescription
                            : null
                    },
                    {
                        "sseConnected", null
                    }
                })
            );
        }

        /**
         * Queue Event that can be aggregated together, where multiple calls are aggregated
         * by incrementing the 'value' field.
         */
        public virtual void QueueAggregateEvent(DevCyclePopulatedUser user, DevCycleEvent @event,
            BucketedUserConfig config, bool throwOnQueueMax = false)
        {
            if (closing)
            {
                return;
            }

            if (CheckEventQueueSize())
            {
                logger.LogWarning("{Event} failed to be queued; events in queue exceed {Max}", @event,
                    localOptions.MaxEventsInQueue);
                if (throwOnQueueMax)
                    throw new DevCycleException(
                        new ErrorResponse("Failed to queue an event. Events in queue exceeded the max"));
                logger.Log(LogLevel.Error, "Failed to queue an event. Events in queue exceeded the max");
                return;
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
            eventCopy.ClientDate = DateTimeOffset.UtcNow.DateTime;
            eventCopy.Value = 1;

            localBucketing.QueueAggregateEvent(
                sdkKey,
                JsonConvert.SerializeObject(@event),
                JsonConvert.SerializeObject(config?.VariableVariationMap ?? new Dictionary<string, FeatureVariation>())
            );
        }

        private IEnumerable<DevCycleRequestEvent> EventsFromAggregateEvents(
            Dictionary<string, Dictionary<string, DevCycleRequestEvent>> aggUserEventsRecord)
        {
            return (from eventType in aggUserEventsRecord
                from eventTarget in eventType.Value
                select eventTarget.Value).ToList();
        }

        private bool CheckEventQueueSize()
        {
            var queueSize = localBucketing.EventQueueSize(sdkKey);
            if (queueSize >= localOptions.FlushEventQueueSize)
            {
                ScheduleFlush();
                if (queueSize >= localOptions.MaxEventsInQueue)
                {
                    return true;
                }
            }

            return false;
        }

        public void ScheduleFlush(bool queueRequest = false)
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

                if (tokenSource.IsCancellationRequested)
                {
                    schedulerIsRunning = false;
                    tokenSource.Token.ThrowIfCancellationRequested();
                }

                await FlushEvents();
                schedulerIsRunning = false;
            }, tokenSource.Token);
        }

        private void OnFlushedEvents(DevCycleEventArgs e)
        {
            if (FlushedEvents?.Target == null) return;
            FlushedEvents?.Invoke(this, e);
        }

        public void Dispose()
        {
            closing = true;
            FlushEvents().GetAwaiter().GetResult();
        }
    }
}