using System;
using System.Collections.Generic;
using RestSharp.Portable;
using System.Threading.Tasks;
using DevCycle.SDK.Server.Common.API;
using DevCycle.SDK.Server.Common.Exception;
using DevCycle.SDK.Server.Common.Model;
using DevCycle.SDK.Server.Common.Model.Cloud;
using DevCycle.SDK.Server.Core.Model;
using Newtonsoft.Json;

namespace DevCycle.Api
{
    public sealed class DVCClient : IDVCClient
    {
        private readonly DVCApiClient apiClient;

        private static readonly string DEFAULT_PLATFORM = "C# Cloud";

        private static readonly string DEFAULT_PLATFORM_VERSION =
            System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;

        private static readonly User.SdkTypeEnum DEFAULT_SDK_TYPE = User.SdkTypeEnum.Server;
        private static readonly string DEFAULT_SDK_VERSION = typeof(DVCClient).Assembly.GetName().Version.ToString();

        public DVCClient(string serverKey)
        {
            apiClient = new DVCApiClient(serverKey);
        }

        public async Task<Dictionary<string, Feature>> AllFeaturesAsync(User user)
        {
            ValidateUser(user);

            AddDefaults(user);

            string urlFragment = "v1/features";

            return await GetResponseAsync<Dictionary<string, Feature>>(user, urlFragment);
        }

        public async Task<IVariable> VariableAsync<T>(User user, string key, T defaultValue)
        {
            ValidateUser(user);

            if (String.IsNullOrEmpty(key))
            {
                throw new ArgumentException("key cannot be null or empty");
            }

            if (defaultValue == null)
            {
                throw new ArgumentNullException("defaultValue cannot be null");
            }

            AddDefaults(user);

            string lowerKey = key.ToLower();

            string urlFragment = "v1/variables/" + lowerKey;

            Variable variable;

            try
            {
                variable = await GetResponseAsync<Variable>(user, urlFragment);
            }
            catch (DVCException e)
            {
                variable = new Variable(lowerKey, (object) defaultValue, e.Message);
            }

            return variable;
        }

        public async Task<Dictionary<string, IVariable>> AllVariablesAsync(User user)
        {
            ValidateUser(user);

            AddDefaults(user);

            string urlFragment = "v1/variables";

            return await GetResponseAsync<Dictionary<string, IVariable>>(user, urlFragment);
        }

        public async Task<DVCResponse> TrackAsync(User user, Event userEvent)
        {
            ValidateUser(user);

            AddDefaults(user);

            string urlFragment = "v1/track";

            UserAndEvents userAndEvents = new UserAndEvents(new List<Event>() {userEvent}, user);

            return await GetResponseAsync<DVCResponse>(userAndEvents, urlFragment);
        }

        private async Task<T> GetResponseAsync<T>(object body, string urlFragment)
        {
            IRestResponse response = null;

            try
            {
                response = await apiClient.SendRequestAsync(body, urlFragment);


                if (response.IsSuccess)
                {
                    if (response.Content != null)
                        return JsonConvert.DeserializeObject<T>(response.Content);
                }

                ErrorResponse errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(response.StatusDescription);
                throw new DVCException(response.StatusCode, errorResponse);
            }
            catch (Exception e)
            {
                if (e.GetType() == typeof(DVCException))
                {
                    throw e;
                }

                ErrorResponse errorResponse = new ErrorResponse(e.ToString());
                throw new DVCException(response.StatusCode, errorResponse);
            }
        }


        private void AddDefaults(User user)
        {
            if (string.IsNullOrEmpty(user.Platform))
            {
                user.Platform = DEFAULT_PLATFORM;
            }

            if (string.IsNullOrEmpty(user.PlatformVersion))
            {
                user.PlatformVersion = DEFAULT_PLATFORM_VERSION;
            }

            if (user.SdkType == null)
            {
                user.SdkType = DEFAULT_SDK_TYPE;
            }

            if (string.IsNullOrEmpty(user.SdkVersion))
            {
                user.SdkVersion = DEFAULT_SDK_VERSION;
            }
        }

        private void ValidateUser(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (user.UserId == String.Empty)
            {
                throw new ArgumentException("userId cannot be empty");
            }
        }

        public void Dispose()
        {
            ((IDisposable) apiClient).Dispose();
        }
    }
}