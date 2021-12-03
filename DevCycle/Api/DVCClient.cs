using System;
using System.Collections.Generic;
using RestSharp.Portable;

using DevCycle.Model;
using System.Threading.Tasks;
using DevCycle.Exception;
using Newtonsoft.Json;

namespace DevCycle.Api
{
    public sealed class DVCClient : IDisposable
    {
        private readonly DVCApiClient apiClient;

        private static readonly string DEFAULT_PLATFORM = "C#";
        private static readonly string DEFAULT_PLATFORM_VERSION = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
        private static readonly User.SdkTypeEnum DEFAULT_SDK_TYPE = User.SdkTypeEnum.Server;
        private static readonly string DEFAULT_SDK_VERSION = "1.0.0";

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

        public async Task<Variable> VariableAsync<T>(User user, string key, T defaultValue)
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

            string urlFragment = "v1/variables/" + key;

            Variable variable;

            try
            {
                variable = await GetResponseAsync<Variable>(user, urlFragment);
            }
            catch(DVCException e)
            {
                variable = new Variable(key, (object)defaultValue, e.Message);
            }
            return variable;
        }

        public async Task<Dictionary<string, Variable>> AllVariablesAsync(User user)
        {
            ValidateUser(user);

            AddDefaults(user);

            string urlFragment = "v1/variables";

            return await GetResponseAsync<Dictionary<string, Variable>>(user, urlFragment);
        }

        public async Task<DVCResponse> TrackAsync(User user, Event userEvent)
        {
            ValidateUser(user);

            AddDefaults(user);

            string urlFragment = "v1/track";

            UserAndEvents userAndEvents = new UserAndEvents(new List<Event>() { userEvent }, user);

            return await GetResponseAsync<DVCResponse>(userAndEvents, urlFragment);
        }

        private async Task<T> GetResponseAsync<T>(Object body, string urlFragment)
        {
            IRestResponse response = null;

            try
            {
                response = await apiClient.SendRequestAsync(body, urlFragment);

                if (!response.IsSuccess)
                {
                    ErrorResponse errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(response.Content);
                    throw new DVCException(response.StatusCode, errorResponse);
                }

                return JsonConvert.DeserializeObject<T>(response.Content);
            }
            catch (System.Exception e)
            {
                if (e.GetType() == typeof(DVCException))
                {
                    throw e;
                }

                ErrorResponse errorResponse = new ErrorResponse(e.Message);
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
                throw new ArgumentNullException("User cannot be null");
            }
            if (user.UserId == String.Empty)
            {
                throw new ArgumentException("userId cannot be empty");
            }
        }

        public void Dispose()
        {
            ((IDisposable)apiClient).Dispose();
        }
    }
}