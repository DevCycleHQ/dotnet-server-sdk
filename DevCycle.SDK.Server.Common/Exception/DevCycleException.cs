using System.Net;
using DevCycle.SDK.Server.Common.Model;

namespace DevCycle.SDK.Server.Common.Exception
{
    public class DevCycleException : System.Exception
    {
        public DevCycleException(HttpStatusCode httpResponseCode, ErrorResponse errorResponse) : base(errorResponse.Message)
        {
            HttpStatusCode = httpResponseCode;
            ErrorResponse = errorResponse;
        }

        public DevCycleException(ErrorResponse errorResponse)
        {
            ErrorResponse = errorResponse;
        }

        public HttpStatusCode HttpStatusCode { get; set; }

        public ErrorResponse ErrorResponse { get; set; }

        public bool IsRetryable()
        {
            return (int)HttpStatusCode >= 500;
        }
    }
}