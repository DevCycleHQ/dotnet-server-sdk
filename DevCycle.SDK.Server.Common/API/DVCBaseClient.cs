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

        protected async Task<T> GetResponseAsync<T>(object body, string urlFragment, Dictionary<string, string> queryParams = null)
        {
            RestResponse response = null;
            try
            {
                response = await GetApiClient().SendRequestAsync(body, urlFragment, queryParams);
                Console.WriteLine(response.Content);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    if (response.Content != null)
                        return JsonConvert.DeserializeObject<T>(response.Content);
                }

                var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(response.Content ?? string.Empty);
                throw new DVCException(response.StatusCode, errorResponse);
            }
            catch (System.Exception e)
            {
                if (e.GetType() == typeof(DVCException))
                {
                    throw e;
                }

                var errorResponse = new ErrorResponse(e.ToString());
                if (response != null) throw new DVCException(response.StatusCode, errorResponse);
                throw new DVCException(errorResponse);
            }
        }
    }
}