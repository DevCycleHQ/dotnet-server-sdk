namespace DevCycle.SDK.Server.Common.Model.Local
{
    public class DVCLocalOptions : IDVCOptions
    {
        public int ConfigPollingIntervalMs { get; }
        public int ConfigPollingTimeoutMs { get; }
        public string CdnUri { get; }
        public bool DisableAutomaticEvents { get; }
        public bool DisableCustomEvents { get; }
        public int MaxEventsInQueue { get; }

        public DVCLocalOptions(int configPollingIntervalMs = 1000, int configPollingTimeoutMs = 5000,
            string cdnUri = "https://config-cdn.devcycle.com", bool disableAutomaticEvents = false, bool disableCustomEvents = false, int maxEventsInQueue = 1000)
        {
            ConfigPollingIntervalMs = configPollingIntervalMs;
            ConfigPollingTimeoutMs = configPollingTimeoutMs;
            CdnUri = cdnUri;
            DisableAutomaticEvents = disableAutomaticEvents;
            DisableCustomEvents = DisableCustomEvents;
            if (maxEventsInQueue > 1000) maxEventsInQueue = 1000;
            MaxEventsInQueue = maxEventsInQueue;
        }
    }
}