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
            int eventFlushIntervalMs = 10 * 1000)
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
            if (maxEventsInQueue > 1000) maxEventsInQueue = 1000;
            MaxEventsInQueue = maxEventsInQueue;
            FlushEventQueueSize = flushEventQueueSize;
            EventFlushIntervalMs = eventFlushIntervalMs;
        }
    }
}