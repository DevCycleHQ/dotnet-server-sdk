namespace DevCycle.SDK.Server.Common.Model.Cloud
{
    public class DVCCloudOptions: IDVCOptions
    {
        public bool EnableEdgeDB { get; private set; }

        public DVCCloudOptions(bool enableEdgeDB = false)
        {
            EnableEdgeDB = enableEdgeDB;
        }
    }
}