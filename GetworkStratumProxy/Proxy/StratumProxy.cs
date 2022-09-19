using GetworkStratumProxy.Node;
using GetworkStratumProxy.Proxy.Client;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace GetworkStratumProxy.Proxy
{
    public class StratumProxy : BaseProxy<StratumProxyClient>
    {
        public override bool IsListening { get; protected set; }
        protected override TcpListener Server { get; set; }

        public StratumProxy(BaseNode node, IPAddress address, int port) : base(node, address, port)
        {
            throw new NotImplementedException();
        }

        protected override Task BeginClientSessionAsync(TcpClient client)
        {
            throw new NotImplementedException();
        }
    }
}
