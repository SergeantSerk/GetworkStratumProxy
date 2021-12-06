using GetworkStratumProxy.Extension;
using GetworkStratumProxy.Node;
using GetworkStratumProxy.Proxy.Client;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace GetworkStratumProxy.Proxy
{
    public abstract class BaseProxy<T> : IProxy, IDisposable where T : BaseClient
    {
        private bool disposedValue;

        protected BaseNode Node { get; private set; }

        public abstract bool IsListening { get; protected set; }
        protected abstract TcpListener Server { get; set; }
        protected ConcurrentDictionary<EndPoint, T> Clients { get; private set; }

        public BaseProxy(BaseNode node, IPAddress address, int port)
        {
            Node = node;
            IsListening = false;
            Clients = new ConcurrentDictionary<EndPoint, T>();
            Server = new TcpListener(address, port);
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
                    Server.BeginAcceptTcpClient(InitialiseTcpClient, Server)
                        .AsyncWaitHandle
                        .WaitOne();
                }
            });
            _ = Task.Run(clientHandleLoopAction);
        }

        private async void InitialiseTcpClient(IAsyncResult ar)
        {
            TcpClient client;
            try
            {
                TcpListener listener = ar.AsyncState as TcpListener;
                client = listener.EndAcceptTcpClient(ar);
            }
            catch (ObjectDisposedException)
            {
                // Safely ignore disposed connections
                ConsoleHelper.Log(GetType().Name, "Could not accept connected client, disposing", LogLevel.Warning);
                return;
            }

            var endpoint = client.Client.RemoteEndPoint;
            ConsoleHelper.Log(GetType().Name, $"{endpoint} connected", LogLevel.Information);

            await BeginClientSessionAsync(client);

            Clients.TryRemove(endpoint, out T clientToRemove);
            ConsoleHelper.Log(GetType().Name, $"{clientToRemove.Endpoint} disconnected", LogLevel.Information);
        }

        protected abstract Task BeginClientSessionAsync(TcpClient client);

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
