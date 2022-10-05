using RestSharp;

namespace DevCycle.SDK.Server.Common.API
{
    public class DVCRestClientOptions : RestClientOptions
    {
        public DVCRestClientOptions Clone()
        {
            return (DVCRestClientOptions) MemberwiseClone();
        }
    }
}