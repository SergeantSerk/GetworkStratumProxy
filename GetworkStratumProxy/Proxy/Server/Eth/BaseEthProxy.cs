using GetworkStratumProxy.Extension;
using GetworkStratumProxy.Node.Eth;
using GetworkStratumProxy.Proxy.Client;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace GetworkStratumProxy.Proxy.Server.Eth
{
    public abstract class BaseProxy<T> : IProxy, IDisposable where T : BaseProxyClient
    {
        private bool disposedValue;

        protected BaseEthNode Node { get; private set; }

        public abstract bool IsListening { get; protected set; }
        protected abstract TcpListener Server { get; set; }
        protected ConcurrentDictionary<EndPoint, T> Clients { get; private set; }

        public BaseProxy(BaseEthNode node, IPAddress address, int port)
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
                    TcpClient client;
                    try
                    {
                        client = Server.AcceptTcpClient();
                    }
                    catch (SocketException e)
                    {
                        if (e.ErrorCode == 10004)
                        {
                            // Server was closed due to WSACancelBlockingCall
                            // likely due to shutdown prompt, safely ignore
                            break;
                        }
                        else
                        {
                            // Unknown error, throw back
                            throw;
                        }
                    }

                    // Set off initialisation of new client and begin listening for new client
                    _ = Task.Run(async () => await InitialiseTcpClientAsync(client))
                    .ContinueWith(_ =>
                    {
                        if (_.Exception != null && _.Exception.InnerException?.InnerException is SocketException e && e.ErrorCode != 10054)
                        {
                            // Ignore client side disconnection
                            ConsoleHelper.Log(GetType().Name, _.Exception.ToString(), LogLevel.Error);
                        }
                    }, TaskScheduler.Current);
                }
            });
            _ = Task.Run(clientHandleLoopAction)
                .ContinueWith(_ =>
                {
                    if (_.Exception != null)
                    {
                        ConsoleHelper.Log(GetType().Name, _.Exception.ToString(), LogLevel.Error);
                    }
                }, TaskScheduler.Current);
        }

        private async Task InitialiseTcpClientAsync(TcpClient client)
        {
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
