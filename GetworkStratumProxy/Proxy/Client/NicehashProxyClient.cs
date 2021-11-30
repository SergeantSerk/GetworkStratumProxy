using System;
using System.Net.Sockets;

namespace GetworkStratumProxy.Proxy.Client
{
    public sealed class NicehashProxyClient : BaseClient
    {
        public NicehashProxyClient(TcpClient tcpClient) : base(tcpClient)
        {
        }

        public override void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
