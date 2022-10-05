using RestSharp;

namespace DevCycle.SDK.Server.Common.API
{
    public class DvcRestClientOptions : RestClientOptions
    {
        public DvcRestClientOptions Clone()
        {
            return (DvcRestClientOptions) MemberwiseClone();
        }
    }
}