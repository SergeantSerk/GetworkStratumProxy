using GetworkStratumProxy.Node;
using GetworkStratumProxy.Proxy.Client;
using System;
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
            throw new NotImplementedException();
        }

        protected override Task BeginClientSessionAsync(TcpClient client)
        {
            throw new NotImplementedException();
        }
    }
}
