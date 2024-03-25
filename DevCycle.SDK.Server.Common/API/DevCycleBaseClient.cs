using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using DevCycle.SDK.Server.Common.Exception;
using DevCycle.SDK.Server.Common.Model;
using Newtonsoft.Json;
using OpenFeature;
using OpenFeature.Model;
using RestSharp;

namespace DevCycle.SDK.Server.Common.API
{
    public abstract class DevCycleBaseClient : IDevCycleClient 
    {
        public string SdkPlatform => $"C#";
        private static string CSharpVersion => System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
        private static string SdkVersion => typeof(IDevCycleClient).Assembly.GetName().Version.ToString();
        private static DevCycleUser.SdkTypeEnum SdkType => DevCycleUser.SdkTypeEnum.Server;
        
        public abstract void Dispose();
        public abstract string Platform();
        public abstract IDevCycleApiClient GetApiClient();
        public abstract DevCycleProvider GetOpenFeatureProvider();
        public abstract Task<Dictionary<string, Feature>> AllFeatures(DevCycleUser user);
        public abstract Task<Dictionary<string, ReadOnlyVariable<object>>> AllVariables(DevCycleUser user);
        public abstract Task<Variable<T>> Variable<T>(DevCycleUser user, string key, T defaultValue);
        public abstract Task<T> VariableValue<T>(DevCycleUser user, string key, T defaultValue);
        public abstract Task<DevCycleResponse> Track(DevCycleUser user, DevCycleEvent userEvent);

        protected void AddDefaults(DevCycleUser user)
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

        protected void ValidateUser(DevCycleUser user)
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
                throw new DevCycleException(response.StatusCode, errorResponse);
            }
            catch (System.Exception e)
            {
                if (e.GetType() == typeof(DevCycleException))
                {
                    throw e;
                }

                errorResponse = new ErrorResponse(e.ToString());
                if (response != null) throw new DevCycleException(response.StatusCode, errorResponse);
                throw new DevCycleException(errorResponse);
            }
        }

        protected void ValidateSDKKey(string sdkKey)
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