using System;
using System.Net.Sockets;

namespace GetworkStratumProxy.Proxy.Eth.Client
{
    public sealed class NicehashEthProxyClient : BaseEthProxyClient
    {
        public NicehashEthProxyClient(TcpClient tcpClient) : base(tcpClient)
        {
            throw new NotImplementedException();
        }

        public override void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
