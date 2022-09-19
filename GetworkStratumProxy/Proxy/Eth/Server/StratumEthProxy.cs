using GetworkStratumProxy.Node.Eth;
using GetworkStratumProxy.Proxy.Eth.Client;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace GetworkStratumProxy.Proxy.Eth.Server
{
    public class StratumEthProxy : BaseEthProxy<StratumEthProxyClient>
    {
        public override bool IsListening { get; protected set; }
        protected override TcpListener Server { get; set; }

        public StratumEthProxy(BaseEthNode node, IPAddress address, int port) : base(node, address, port)
        {
            throw new NotImplementedException();
        }

        protected override Task BeginClientSessionAsync(TcpClient client)
        {
            throw new NotImplementedException();
        }
    }
}
