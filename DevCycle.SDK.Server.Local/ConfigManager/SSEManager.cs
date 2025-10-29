using System;
using LaunchDarkly.EventSource;

namespace DevCycle.SDK.Server.Local.ConfigManager
{
    public class SSEManager : IDisposable
    {
        private EventSource sseClient { get; set; }
        private string sseUri { get; set; }
        private readonly EventHandler<StateChangedEventArgs> stateHandler;
        private readonly EventHandler<MessageReceivedEventArgs> messageHandler;
        private readonly EventHandler<ExceptionEventArgs> errorHandler;
        private bool disposed = false;
        
        public SSEManager(string sseUri, EventHandler<StateChangedEventArgs> stateHandler,
            EventHandler<MessageReceivedEventArgs> messageHandler, EventHandler<ExceptionEventArgs> errorHandler)
        {
            var sseConfig = Configuration.Builder(new Uri(sseUri)).InitialRetryDelay(new TimeSpan(0, 0, 10)).Build();
            sseClient = new EventSource(sseConfig);
            this.sseUri = sseUri;
            this.stateHandler = stateHandler;
            this.messageHandler = messageHandler;
            this.errorHandler = errorHandler;
            
            AttachHandlers(sseClient);
        }

        private void AttachHandlers(EventSource client)
        {
            client.Closed += stateHandler;
            client.Opened += stateHandler;
            client.Error += errorHandler;
            client.MessageReceived += messageHandler;
        }

        private void DetachHandlers(EventSource client)
        {
            client.Closed -= stateHandler;
            client.Opened -= stateHandler;
            client.Error -= errorHandler;
            client.MessageReceived -= messageHandler;
        }

        public virtual void StartSSE()
        {
            sseClient.StartAsync();
        }
        public virtual void RestartSSE(string uri = null, bool resetBackoffDelay = true)
        {
            if (disposed) return;
            if (!string.IsNullOrEmpty(uri) && uri != sseUri)
            {
                sseUri = uri;
                try
                {
                    DetachHandlers(sseClient);
                    sseClient.Close();
                    (sseClient as IDisposable)?.Dispose();
                }
                catch {}
                sseClient = new EventSource(Configuration.Builder(new Uri(uri))
                    .InitialRetryDelay(new TimeSpan(0, 0, 10)).Build());
                AttachHandlers(sseClient);
                StartSSE();
            }
            else
            {
                sseClient.Restart(resetBackoffDelay);
            }
        }

        public void CloseSSE()
        {
            if (disposed) return;
            try
            {
                sseClient.Close();
            }
            catch {}
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;
            disposed = true;
            if (disposing)
            {
                try
                {
                    DetachHandlers(sseClient);
                    sseClient.Close();
                    (sseClient as IDisposable)?.Dispose();
                }
                catch {}
            }
        }

    }
}