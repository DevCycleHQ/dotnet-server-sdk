using System;
using System.Collections.Generic;

namespace DevCycle.SDK.Server.Common.Model.Local
{
    public class DVCLocalOptions : IDVCOptions
    {
        public int ConfigPollingIntervalMs { get; set; }
        public int ConfigPollingTimeoutMs { get; set; }
        public string CdnUri { get; set; }
        public string CdnSlug { get; set; }

        public bool DisableAutomaticEvents { get; set; }
        public bool DisableCustomEvents { get; set; }
        public int MaxEventsInQueue { get; set; }
        public int EventRequestChunkSize { get; set; }
        public int FlushEventQueueSize { get; set; }
        public int EventFlushIntervalMs { get; set; }
        public string EventsApiUri { get; set; }

        public string EventsApiSlug { get; set; }
        public Dictionary<string, string> CdnCustomHeaders { get; set; }
        public Dictionary<string, string> EventsApiCustomHeaders { get; set; }

        public DVCLocalOptions(
            int configPollingIntervalMs = 1000,
            int configPollingTimeoutMs = 5000,
            string cdnUri = "https://config-cdn.devcycle.com",
            string cdnSlug = "",
            string eventsApiUri = "https://events.devcycle.com",
            string eventsApiSlug = "/v1/events/batch",
            Dictionary<string, string> cdnCustomHeaders = null,
            Dictionary<string, string> eventsApiCustomHeaders = null,
            bool disableAutomaticEvents = false,
            bool disableCustomEvents = false,
            int flushEventQueueSize = 1000,
            int maxEventsInQueue = 2000,
            int eventRequestChunkSize = 100,
            int eventFlushIntervalMs = 10 * 1000
            )
        {
            ConfigPollingIntervalMs = configPollingIntervalMs;
            ConfigPollingTimeoutMs = configPollingTimeoutMs;
            CdnUri = cdnUri;
            CdnSlug = cdnSlug;
            EventsApiUri = eventsApiUri;
            EventsApiSlug = eventsApiSlug;
            CdnCustomHeaders = cdnCustomHeaders;
            EventsApiCustomHeaders = eventsApiCustomHeaders;
            DisableAutomaticEvents = disableAutomaticEvents;
            DisableCustomEvents = disableCustomEvents;
            
            switch (eventRequestChunkSize)
            {
                case < 10:
                    throw new ArgumentOutOfRangeException(
                        nameof(eventRequestChunkSize),
                        eventRequestChunkSize,
                        "Must be greater than or equal to 10");
                case > 10000:
                    throw new ArgumentOutOfRangeException(
                        nameof(eventRequestChunkSize),
                        eventRequestChunkSize,
                        "Must not be larger than 10,000");
            }
            EventRequestChunkSize = eventRequestChunkSize;
            
            if (maxEventsInQueue > 20000)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxEventsInQueue),
                    maxEventsInQueue,
                    "Must be less than or equal to 20,000");
            }
            if (maxEventsInQueue < eventRequestChunkSize)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxEventsInQueue),
                    maxEventsInQueue,
                    $"Must be greater than or equal to eventRequestChunkSize ({eventRequestChunkSize})");
            }
            MaxEventsInQueue = maxEventsInQueue;

            if (flushEventQueueSize >= maxEventsInQueue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(flushEventQueueSize),
                    flushEventQueueSize,
                    $"Must be smaller than maxEventsInQueue ({maxEventsInQueue})");
            }
            if (flushEventQueueSize < eventRequestChunkSize)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(flushEventQueueSize),
                    flushEventQueueSize,
                    $"Must be greater than or equal to eventRequestChunkSize ({eventRequestChunkSize})");
            }
            FlushEventQueueSize = flushEventQueueSize;
            
            EventFlushIntervalMs = eventFlushIntervalMs;
            switch (eventFlushIntervalMs)
            {
                case < 500:
                    throw new ArgumentOutOfRangeException(
                        nameof(eventFlushIntervalMs),
                        eventFlushIntervalMs,
                        $"Must be larger than 500ms");
                case > 60 * 1000:
                    throw new ArgumentOutOfRangeException(
                        nameof(eventFlushIntervalMs),
                        eventFlushIntervalMs,
                        $"Must be smaller than 1 minute");
            }
        }
    }
}