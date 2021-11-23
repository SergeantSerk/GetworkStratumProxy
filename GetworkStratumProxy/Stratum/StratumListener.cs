using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace GetworkStratumProxy.Stratum
{
    public class StratumListener : TcpListener
    {
        public ConcurrentDictionary<EndPoint, StratumClient> StratumClients { get; private set; }

        public StratumListener(IPAddress address, int port) : base(address, port)
        {
            StratumClients = new ConcurrentDictionary<EndPoint, StratumClient>();
        }
    }
}
