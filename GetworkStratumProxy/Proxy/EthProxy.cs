using GetworkStratumProxy.Extension;
using GetworkStratumProxy.Node;
using GetworkStratumProxy.Proxy.Client;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace GetworkStratumProxy.Proxy
{
    public sealed class EthProxy : BaseProxy<EthProxyClient>
    {
        public override bool IsListening { get; protected set; }
        protected override TcpListener Server { get; set; }

        public EthProxy(BaseNode node, IPAddress address, int port) : base(node, address, port)
        {

        }

        protected override async Task BeginClientSessionAsync(TcpClient client)
        {
            var endpoint = client.Client.RemoteEndPoint;
            using EthProxyClient proxyClient = GetClientOrNew(client);
            Node.NewWorkReceived += proxyClient.NewWorkNotificationEvent;  // Subscribe to new work
            await proxyClient.StartListeningAsync(); // Blocking listen
            Node.NewWorkReceived -= proxyClient.NewWorkNotificationEvent;  // Unsubscribe
            ConsoleHelper.Log(GetType().Name, $"Client {endpoint} unsubscribed from receiving new work", LogLevel.Information);
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
