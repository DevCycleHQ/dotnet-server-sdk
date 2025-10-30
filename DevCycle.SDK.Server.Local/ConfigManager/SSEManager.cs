using System;
using System.Threading.Tasks;
using LaunchDarkly.EventSource;

namespace DevCycle.SDK.Server.Local.ConfigManager
{
    public class SSEManager : IDisposable
    {
        private EventSource sseClient { get; set; }
        private string sseUri { get; set; }
        private EventHandler<StateChangedEventArgs> stateHandler { get; }
        private EventHandler<MessageReceivedEventArgs> messageHandler { get; }
        private EventHandler<ExceptionEventArgs> errorHandler { get; }
        
        public SSEManager(string sseUri, EventHandler<StateChangedEventArgs> stateHandler,
            EventHandler<MessageReceivedEventArgs> messageHandler, EventHandler<ExceptionEventArgs> errorHandler)
        {
            var sseConfig = Configuration.Builder(new Uri(sseUri)).InitialRetryDelay(TimeSpan.FromSeconds(10)).Build();
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
                    .InitialRetryDelay(TimeSpan.FromSeconds(10)).Build());
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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (sseClient != null)
                {
                    // Unsubscribe event handlers
                    sseClient.Closed -= stateHandler;
                    sseClient.Opened -= stateHandler;
                    sseClient.Error -= errorHandler;
                    sseClient.MessageReceived -= messageHandler;

                    // Dispose or Close sseClient
                    if (sseClient is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    else
                    {
                        sseClient.Close();
                    }
                    sseClient = null;
                }
            }
        }
    }
}