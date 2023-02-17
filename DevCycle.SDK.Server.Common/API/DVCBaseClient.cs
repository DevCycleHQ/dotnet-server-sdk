using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using DevCycle.SDK.Server.Common.Exception;
using DevCycle.SDK.Server.Common.Model;
using Newtonsoft.Json;
using RestSharp;

namespace DevCycle.SDK.Server.Common.API
{
    public abstract class DVCBaseClient : IDVCClient
    {
        private string SdkPlatform => $"C# {Platform()}";
        private static string CSharpVersion => System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
        private static string SdkVersion => typeof(IDVCClient).Assembly.GetName().Version.ToString();
        private static User.SdkTypeEnum SdkType => User.SdkTypeEnum.Server;
        public abstract void Dispose();
        public abstract string Platform();
        public abstract IDVCApiClient GetApiClient();

        protected void AddDefaults(User user)
        {
            if (string.IsNullOrEmpty(user.Platform))
            {
                user.Platform = SdkPlatform;
            }

            if (string.IsNullOrEmpty(user.PlatformVersion))
            {
                user.PlatformVersion = CSharpVersion;
            }

            user.SdkType ??= SdkType;

            if (string.IsNullOrEmpty(user.SdkVersion))
            {
                user.SdkVersion = SdkVersion;
            }
        }

        protected void ValidateUser(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (user.UserId == string.Empty)
            {
                throw new ArgumentException("userId cannot be empty");
            }
        }

        protected async Task<T> GetResponseAsync<T>(
            object body,
            string urlFragment, 
            Dictionary<string, string> queryParams = null,
            bool shouldRetry = true
        )  {
            RestResponse response = null;
            ErrorResponse errorResponse = null;
            try
            {
                response = await GetApiClient().SendRequestAsync(body, urlFragment, queryParams, shouldRetry);
                if (response.IsSuccessful)
                {
                    if (response.Content != null)
                    {
                        var deserializedResponse = JsonConvert.DeserializeObject<T>(response.Content);
                        if (deserializedResponse != null)
                        {
                            return deserializedResponse;
                        }
                    }
                }

                if (response.Content != null)
                {
                    errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(response.Content);
                }
                
                errorResponse ??= new ErrorResponse("Unexpected Error Occurred"); 
                throw new DVCException(response.StatusCode, errorResponse);
            }
            catch (System.Exception e)
            {
                if (e.GetType() == typeof(DVCException))
                {
                    throw e;
                }

                errorResponse = new ErrorResponse(e.ToString());
                if (response != null) throw new DVCException(response.StatusCode, errorResponse);
                throw new DVCException(errorResponse);
            }
        }

        public void ValidateSDKKey(string sdkKey)
        {
            if (string.IsNullOrEmpty(sdkKey))
            {
                throw new ArgumentException("Missing SDK key! Call build with a valid SDK key");
            }

            if (!sdkKey.StartsWith("server") && !sdkKey.StartsWith("dvc_server"))
            {
                throw new ArgumentException("Invalid SDK key provided. Please call build with a valid server SDK key");
            }
        }
    }
}