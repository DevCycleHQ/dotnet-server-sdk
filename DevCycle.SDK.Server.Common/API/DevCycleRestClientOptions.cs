using RestSharp;

namespace DevCycle.SDK.Server.Common.API
{
    public class DevCycleRestClientOptions : RestClientOptions
    {
        public DevCycleRestClientOptions Clone()
        {
            return (DevCycleRestClientOptions) MemberwiseClone();
        }
    }
}