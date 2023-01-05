using Polly;
using Polly.Retry;
using Polly.Timeout;
using Polly.Wrap;
using RestSharp;
using System;

namespace DevCycle.SDK.Server.Common.Policies
{
    public class ClientPolicy
    {
        public AsyncPolicyWrap<RestResponse> RetryPolicyWithTimeout { get; }
        private static ClientPolicy _instance = new ClientPolicy();

        private ClientPolicy()
        {
            AsyncTimeoutPolicy timeoutPolicy = Policy.TimeoutAsync(5, TimeoutStrategy.Pessimistic);
            AsyncRetryPolicy<RestResponse> retryPolicy = Policy
                .HandleResult<RestResponse>(res => (int)res.StatusCode >= 500)
                .WaitAndRetryAsync(5, retryAttempt => {
                  var delay = Math.Pow(2, retryAttempt) * 100;
                  var randomSum = delay * 0.2 * new Random().NextDouble();
                  return TimeSpan.FromMilliseconds(delay + randomSum);
                });
            RetryPolicyWithTimeout = retryPolicy.WrapAsync(timeoutPolicy);
        }

        public static ClientPolicy GetInstance() => _instance;
    }
}