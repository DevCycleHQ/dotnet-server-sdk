using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevCycle.SDK.Server.Common.API;
using DevCycle.SDK.Server.Common.Exception;
using DevCycle.SDK.Server.Common.Model;
using DevCycle.SDK.Server.Common.Model.Cloud;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;


namespace DevCycle.SDK.Server.Cloud.Api
{

    public class DVCCloudClientBuilder : DVCClientBuilder
    {
        public override IDVCClient Build()
        {
            return new DVCCloudClient(environmentKey, loggerFactory, options, restClientOptions);
        }
    }
    public sealed class DVCCloudClient : DVCBaseClient
    {
        private readonly DVCApiClient apiClient;
        private readonly ILogger logger;

        private readonly DVCCloudOptions options;

        internal DVCCloudClient(string serverKey, ILoggerFactory loggerFactory, IDVCOptions options=null, DVCRestClientOptions restClientOptions = null)
        {
            apiClient = new DVCApiClient(serverKey, restClientOptions);
            logger = loggerFactory.CreateLogger<DVCCloudClient>();
            this.options = options != null ? (DVCCloudOptions) options : new DVCCloudOptions();
        }
        
        public override string Platform()
        {
            return "Cloud"; 
        }

        public override IDVCApiClient GetApiClient()
        {
            return apiClient;
        }

        public async Task<Dictionary<string, Feature>> AllFeaturesAsync(User user)
        {
            ValidateUser(user);

            AddDefaults(user);

            string urlFragment = "v1/features";
            var queryParams = new Dictionary<string, string>();
            if (options.EnableEdgeDB) queryParams.Add("enableEdgeDB", "true");
            
            return await GetResponseAsync<Dictionary<string, Feature>>(user, urlFragment, queryParams);
        }

        public async Task<IVariable> VariableAsync<T>(User user, string key, T defaultValue)
        {
            ValidateUser(user);

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("key cannot be null or empty");
            }

            if (defaultValue == null)
            {
                throw new ArgumentNullException(nameof(defaultValue));
            }

            AddDefaults(user);

            string lowerKey = key.ToLower();

            string urlFragment = "v1/variables/" + lowerKey;
            var queryParams = new Dictionary<string, string>();
            if (options.EnableEdgeDB) queryParams.Add("enableEdgeDB", "true");

            Variable variable;

            try
            {
                variable = await GetResponseAsync<Variable>(user, urlFragment, queryParams);
            }
            catch (DVCException e)
            {
                variable = new Variable(lowerKey, (object) defaultValue, e.Message);
            }

            return variable;
        }

        public async Task<Dictionary<string, Variable>> AllVariablesAsync(User user)
        {
            ValidateUser(user);

            AddDefaults(user);

            string urlFragment = "v1/variables";
            var queryParams = new Dictionary<string, string>();
            if (options.EnableEdgeDB) queryParams.Add("enableEdgeDB", "true");


            return await GetResponseAsync<Dictionary<string, Variable>>(user, urlFragment, queryParams);
        }

        public async Task<DVCResponse> TrackAsync(User user, Event userEvent)
        {
            ValidateUser(user);

            AddDefaults(user);

            string urlFragment = "v1/track";
            var queryParams = new Dictionary<string, string>();
            if (options.EnableEdgeDB) queryParams.Add("enableEdgeDB", "true");

            UserAndEvents userAndEvents = new UserAndEvents(new List<Event>() {userEvent}, user);

            return await GetResponseAsync<DVCResponse>(userAndEvents, urlFragment, queryParams);
        }

        public override void Dispose()
        {
            ((IDisposable) apiClient).Dispose();
        }
    }
}