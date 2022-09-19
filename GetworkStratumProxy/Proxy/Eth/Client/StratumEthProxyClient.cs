using System;
using System.Net.Sockets;

namespace GetworkStratumProxy.Proxy.Eth.Client
{
    public sealed class StratumEthProxyClient : BaseEthProxyClient
    {
        public StratumEthProxyClient(TcpClient tcpClient) : base(tcpClient)
        {
            throw new NotImplementedException();
        }

        public override void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
