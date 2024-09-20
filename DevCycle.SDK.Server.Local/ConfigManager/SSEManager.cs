using System;
using System.Threading.Tasks;
using LaunchDarkly.EventSource;

namespace DevCycle.SDK.Server.Local.ConfigManager
{
    public class SSEManager
    {
        private EventSource sseClient { get; set; }
        private string sseUri { get; set; }
        private EventHandler<StateChangedEventArgs> stateHandler { get; set; }
        private EventHandler<MessageReceivedEventArgs> messageHandler { get; set; }
        private EventHandler<ExceptionEventArgs> errorHandler { get; set; }
        
        
        public SSEManager(string sseUri, EventHandler<StateChangedEventArgs> stateHandler,
            EventHandler<MessageReceivedEventArgs> messageHandler, EventHandler<ExceptionEventArgs> errorHandler)
        {
            var sseConfig = Configuration.Builder(new Uri(sseUri)).InitialRetryDelay(new TimeSpan(0, 0, 10)).Build();
            sseClient = new EventSource(sseConfig);
            this.sseUri = sseUri;
            this.stateHandler = stateHandler;
            this.messageHandler = messageHandler;
            this.errorHandler = errorHandler;
            
            sseClient.Closed += stateHandler;
            sseClient.Opened += stateHandler;
            sseClient.Error += errorHandler;
            sseClient.MessageReceived += messageHandler;
        }

        public void StartSSE()
        {
            sseClient.StartAsync();
        }
        public void RestartSSE(string uri = null, bool resetBackoffDelay = true)
        {
            if (uri != null && uri != sseUri && uri != "")
            {
                sseUri = uri;
                sseClient.Close();
                
                sseClient = new EventSource(Configuration.Builder(new Uri(uri))
                    .InitialRetryDelay(new TimeSpan(0, 0, 10)).Build());
                sseClient.MessageReceived += messageHandler;
                sseClient.Error += errorHandler;
                sseClient.Closed += stateHandler;
                sseClient.Opened += stateHandler;
                StartSSE();
            }
            else
            {
                sseClient.Restart(resetBackoffDelay);
            }
        }

        public void CloseSSE()
        {
            sseClient.Close();
        }

    }
}