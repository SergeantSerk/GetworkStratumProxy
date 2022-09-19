using GetworkStratumProxy.Extension;
using GetworkStratumProxy.Node;
using GetworkStratumProxy.Proxy.Client;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace GetworkStratumProxy.Proxy
{
    public class NicehashProxy : BaseProxy<NicehashProxyClient>
    {
        public override bool IsListening { get; protected set; }
        protected override TcpListener Server { get; set; }

        public NicehashProxy(BaseNode node, IPAddress address, int port) : base(node, address, port)
        {

        }

        protected override async Task BeginClientSessionAsync(TcpClient client)
        {
            var endpoint = client.Client.RemoteEndPoint;
            using NicehashProxyClient proxyClient = GetClientOrNew(client);
            Node.NewWorkReceived += proxyClient.NewJobNotificationEvent;  // Subscribe to new jobs
            await proxyClient.StartListeningAsync(); // Blocking listen
            Node.NewWorkReceived -= proxyClient.NewJobNotificationEvent;  // Unsubscribe
            ConsoleHelper.Log(GetType().Name, $"Client {endpoint} unsubscribed from jobs", LogLevel.Information);
        }

        private NicehashProxyClient GetClientOrNew(TcpClient tcpClient)
        {
            if (!Clients.TryGetValue(tcpClient.Client.RemoteEndPoint, out NicehashProxyClient nicehashProxyClient))
            {
                // Remote endpoint not registered, add new client
                ConsoleHelper.Log(GetType().Name, $"Registered new client {tcpClient.Client.RemoteEndPoint}", LogLevel.Debug);
                nicehashProxyClient = new NicehashProxyClient(tcpClient, Node.Web3.Eth.Mining.GetWork, Node.Web3.Eth.Mining.SubmitWork);
                Clients.TryAdd(tcpClient.Client.RemoteEndPoint, nicehashProxyClient);
            }
            return nicehashProxyClient;
        }
    }
}
