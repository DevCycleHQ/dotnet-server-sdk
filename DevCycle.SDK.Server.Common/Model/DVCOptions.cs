namespace DevCycle.SDK.Server.Common.Model
{
    public class DVCOptions
    {
        public int ConfigPollingIntervalMs { get; private set; }
        public int ConfigPollingTimeoutMs { get; private set; }
        public string CdnUri { get; private set; }

        public DVCOptions(int configPollingIntervalMs = 1000, int configPollingTimeoutMs = 5000,
            string cdnUri = "https://config-cdn.devcycle.com")
        {
            ConfigPollingIntervalMs = configPollingIntervalMs;
            ConfigPollingTimeoutMs = configPollingTimeoutMs;
            CdnUri = cdnUri;
        }
    }
}