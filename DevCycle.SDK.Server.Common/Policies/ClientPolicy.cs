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
        public AsyncPolicyWrap<RestResponse> ExponentialBackoffRetryPolicyWithTimeout { get; }
        public AsyncRetryPolicy<RestResponse> RetryOncePolicy { get; }
        public AsyncTimeoutPolicy TimeoutPolicy { get; }
        private static ClientPolicy _instance = new ClientPolicy();

        private ClientPolicy()
        {
            TimeoutPolicy = Policy.TimeoutAsync(5, TimeoutStrategy.Pessimistic);
            AsyncRetryPolicy<RestResponse> exponentialBackoffRetryPolicy = Policy
                .HandleResult<RestResponse>(res => (int)res.StatusCode >= 500)
                .WaitAndRetryAsync(5, retryAttempt => {
                  var delay = Math.Pow(2, retryAttempt) * 100;
                  var randomSum = delay * 0.2 * new Random().NextDouble();
                  return TimeSpan.FromMilliseconds(delay + randomSum);
                });
            ExponentialBackoffRetryPolicyWithTimeout = exponentialBackoffRetryPolicy.WrapAsync(TimeoutPolicy);

            RetryOncePolicy = Policy
                .HandleResult<RestResponse>(res => (int)res.StatusCode >= 500)
                .RetryAsync(1);
        }

        public static ClientPolicy GetInstance() => _instance;
    }
}