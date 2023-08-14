namespace DevCycle.SDK.Server.Common.Model.Cloud
{
    public class DevCycleCloudOptions: IDevCycleOptions
    {
        public bool EnableEdgeDB { get; private set; }

        public DevCycleCloudOptions(bool enableEdgeDB = false)
        {
            EnableEdgeDB = enableEdgeDB;
        }
    }
}