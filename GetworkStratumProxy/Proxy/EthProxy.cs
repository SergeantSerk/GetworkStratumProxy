using GetworkStratumProxy.Extension;
using GetworkStratumProxy.Node;
using GetworkStratumProxy.Proxy.Client;
using System;
using System.Net;
using System.Net.Sockets;

namespace GetworkStratumProxy.Proxy
{
    public sealed class EthProxy : BaseProxy<EthProxyClient>
    {
        public override bool IsListening { get; protected set; }
        protected override TcpListener Server { get; set; }

        public EthProxy(BaseNode node, IPAddress address, int port) : base(node)
        {
            Server = new TcpListener(address, port);
        }

        protected override async void HandleTcpClient(IAsyncResult ar)
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

            using (EthProxyClient proxyClient = GetClientOrNew(client))
            {
                Node.NewJobReceived += proxyClient.NewJobNotificationEvent;  // Subscribe to new jobs
                await proxyClient.StartListeningAsync(); // Blocking listen
                Node.NewJobReceived -= proxyClient.NewJobNotificationEvent;  // Unsubscribe
                ConsoleHelper.Log(GetType().Name, $"Client {endpoint} unsubscribed from jobs", LogLevel.Information);
            }

            Clients.TryRemove(endpoint, out EthProxyClient clientToRemove);
            ConsoleHelper.Log(GetType().Name, $"{clientToRemove.Endpoint} disconnected", LogLevel.Information);
        }

        private EthProxyClient GetClientOrNew(TcpClient tcpClient)
        {
            if (!Clients.TryGetValue(tcpClient.Client.RemoteEndPoint, out EthProxyClient ethProxyClient))
            {
                // Remote endpoint not registered, add new client
                ConsoleHelper.Log(GetType().Name, $"Registered new client {tcpClient.Client.RemoteEndPoint}", LogLevel.Debug);
                ethProxyClient = new EthProxyClient(tcpClient, Node.Web3.Eth.Mining.GetWork, Node.Web3.Eth.Mining.SubmitWork);
                Clients.TryAdd(tcpClient.Client.RemoteEndPoint, ethProxyClient);
            }
            return ethProxyClient;
        }
    }
}
