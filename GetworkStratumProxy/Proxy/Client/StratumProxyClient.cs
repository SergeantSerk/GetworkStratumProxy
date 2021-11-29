using System;
using System.Net.Sockets;

namespace GetworkStratumProxy.Proxy.Client
{
    public sealed class StratumProxyClient : BaseClient
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
