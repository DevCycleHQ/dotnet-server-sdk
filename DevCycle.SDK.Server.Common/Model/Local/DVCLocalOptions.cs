using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace DevCycle.SDK.Server.Common.Model.Local
{
    public class DVCLocalOptions : IDVCOptions
    {
        [DataMember(Name="configPollingIntervalMS", EmitDefaultValue=false)]
        [JsonProperty("configPollingIntervalMS")]
        public int ConfigPollingIntervalMs { get; set; }
        
        [DataMember(Name="configPollingTimeoutMS", EmitDefaultValue=false)]
        [JsonProperty("configPollingTimeoutMS")]
        public int ConfigPollingTimeoutMs { get; set; }
        
        [IgnoreDataMember]
        public string CdnUri { get; set; }
        
        [IgnoreDataMember]
        public string CdnSlug { get; set; }

        [DataMember(Name="disableAutomaticEventLogging", EmitDefaultValue=false)]
        [JsonProperty("disableAutomaticEventLogging")]
        public bool DisableAutomaticEvents { get; set; }
        
        [DataMember(Name="disableCustomEventLogging", EmitDefaultValue=false)]
        [JsonProperty("disableCustomEventLogging")]
        public bool DisableCustomEvents { get; set; }
        
        [IgnoreDataMember]
        public int MaxEventsInQueue { get; set; }
        
        [DataMember(Name="eventRequestChunkSize", EmitDefaultValue=false)]
        [JsonProperty("eventRequestChunkSize")]
        public int EventRequestChunkSize { get; set; }
        
        [IgnoreDataMember]
        public int FlushEventQueueSize { get; set; }
        
        [IgnoreDataMember]
        public int EventFlushIntervalMs { get; set; }
        
        [IgnoreDataMember]
        public string EventsApiUri { get; set; }

        [IgnoreDataMember]
        public string EventsApiSlug { get; set; }
        
        [IgnoreDataMember]
        public Dictionary<string, string> CdnCustomHeaders { get; set; }
        
        [IgnoreDataMember]
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