using System;
using System.Net;
using DevCycle.Model;

namespace DevCycle.Exception
{
    public class DVCException : System.Exception
    {
        public DVCException(HttpStatusCode httpResponseCode, ErrorResponse errorResponse) : base(errorResponse.Message)
        {
            HttpStatusCode = httpResponseCode;
            ErrorResponse = errorResponse;
        }

        public HttpStatusCode HttpStatusCode { get; set; }

        public ErrorResponse ErrorResponse { get; set; }
    }
}
