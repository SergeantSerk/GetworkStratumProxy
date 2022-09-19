using System;
using System.Net.Sockets;

namespace GetworkStratumProxy.Proxy.Client.Eth
{
    public sealed class StratumProxyClient : BaseProxyClient
    {
        public StratumProxyClient(TcpClient tcpClient) : base(tcpClient)
        {
        }

        public override void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
