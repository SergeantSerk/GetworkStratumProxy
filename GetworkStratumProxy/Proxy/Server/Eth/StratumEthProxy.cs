using GetworkStratumProxy.Node.Eth;
using GetworkStratumProxy.Proxy.Client.Eth;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace GetworkStratumProxy.Proxy.Server.Eth
{
    public class StratumProxy : BaseEthProxy<StratumEthProxyClient>
    {
        public override bool IsListening { get; protected set; }
        protected override TcpListener Server { get; set; }

        public StratumProxy(BaseEthNode node, IPAddress address, int port) : base(node, address, port)
        {
            throw new NotImplementedException();
        }

        protected override Task BeginClientSessionAsync(TcpClient client)
        {
            throw new NotImplementedException();
        }
    }
}
