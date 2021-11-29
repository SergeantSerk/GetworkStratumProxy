using GetworkStratumProxy.Extension;
using GetworkStratumProxy.Node;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace GetworkStratumProxy.Proxy
{
    public abstract class BaseProxy : IProxy, IDisposable
    {
        private bool disposedValue;

        protected BaseNode Node { get; private set; }

        public abstract bool IsListening { get; protected set; }
        protected abstract TcpListener Server { get; set; }
        protected ConcurrentDictionary<EndPoint, StratumClient> Clients { get; private set; }

        public BaseProxy(BaseNode node)
        {
            Node = node;
            IsListening = false;
            Clients = new ConcurrentDictionary<EndPoint, StratumClient>();
        }

        public void Start()
        {
            ConsoleHelper.Log(GetType().Name, $"Listening on {Server.LocalEndpoint}", LogLevel.Information);
            IsListening = true;
            Server.Start();

            var clientHandleLoopAction = new Action(() =>
            {
                while (IsListening)
                {
                    Server.BeginAcceptTcpClient(HandleTcpClient, Server)
                        .AsyncWaitHandle
                        .WaitOne();
                }
            });
            _ = Task.Run(clientHandleLoopAction);
        }

        protected abstract void HandleTcpClient(IAsyncResult ar);

        public void Stop()
        {
            IsListening = false;
            Server.Stop();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    ConsoleHelper.Log(GetType().Name, "Shutting down server", LogLevel.Information);
                    IsListening = false;
                    Server.Stop();
                    // Disconnect each client
                    foreach (var client in Clients)
                    {
                        client.Value.Dispose();
                        ConsoleHelper.Log(GetType().Name, $"Disconnecting client {client.Key}", LogLevel.Information);
                    }
                }

                Server = null;
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
