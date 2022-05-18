using System.Net;
using DevCycle.SDK.Server.Common.Model;

namespace DevCycle.SDK.Server.Common.Exception
{
    public class DVCException : System.Exception
    {
        public DVCException(HttpStatusCode httpResponseCode, ErrorResponse errorResponse) : base(errorResponse.Message)
        {
            HttpStatusCode = httpResponseCode;
            ErrorResponse = errorResponse;
        }

        public DVCException(ErrorResponse errorResponse)
        {
            ErrorResponse = errorResponse;
        }

        public HttpStatusCode HttpStatusCode { get; set; }

        public ErrorResponse ErrorResponse { get; set; }
    }
}