using GetworkStratumProxy.Extension;
using GetworkStratumProxy.Node.Eth;
using GetworkStratumProxy.Proxy.Eth.Client;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace GetworkStratumProxy.Proxy.Eth.Server
{
    public sealed class GetworkEthProxy : BaseEthProxy<GetworkEthProxyClient>
    {
        public override bool IsListening { get; protected set; }
        protected override TcpListener Server { get; set; }

        public GetworkEthProxy(BaseEthNode node, IPAddress address, int port) : base(node, address, port)
        {

        }

        protected override async Task BeginClientSessionAsync(TcpClient client)
        {
            var endpoint = client.Client.RemoteEndPoint;
            using GetworkEthProxyClient proxyClient = GetClientOrNew(client);
            Node.NewWorkReceived += proxyClient.NewWorkNotificationEvent;  // Subscribe to new work
            await proxyClient.StartListeningAsync(); // Blocking listen
            Node.NewWorkReceived -= proxyClient.NewWorkNotificationEvent;  // Unsubscribe
            ConsoleHelper.Log(GetType().Name, $"Client {endpoint} unsubscribed from receiving new work", LogLevel.Information);
        }

        private GetworkEthProxyClient GetClientOrNew(TcpClient tcpClient)
        {
            if (!Clients.TryGetValue(tcpClient.Client.RemoteEndPoint, out GetworkEthProxyClient ethProxyClient))
            {
                // Remote endpoint not registered, add new client
                ConsoleHelper.Log(GetType().Name, $"Registered new client {tcpClient.Client.RemoteEndPoint}", LogLevel.Debug);
                ethProxyClient = new GetworkEthProxyClient(tcpClient, Node.Web3.Eth.Mining.GetWork, Node.Web3.Eth.Mining.SubmitWork);
                Clients.TryAdd(tcpClient.Client.RemoteEndPoint, ethProxyClient);
            }
            return ethProxyClient;
        }
    }
}
