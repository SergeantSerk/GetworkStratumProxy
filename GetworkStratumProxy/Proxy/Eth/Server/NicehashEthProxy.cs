using GetworkStratumProxy.Node.Eth;
using GetworkStratumProxy.Proxy.Eth.Client;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace GetworkStratumProxy.Proxy.Eth.Server
{
    public class NicehashEthProxy : BaseEthProxy<NicehashEthProxyClient>
    {
        public override bool IsListening { get; protected set; }
        protected override TcpListener Server { get; set; }

        public NicehashEthProxy(BaseEthNode node, IPAddress address, int port) : base(node, address, port)
        {
            throw new NotImplementedException();
        }

        protected override Task BeginClientSessionAsync(TcpClient client)
        {
            throw new NotImplementedException();
        }
    }
}
