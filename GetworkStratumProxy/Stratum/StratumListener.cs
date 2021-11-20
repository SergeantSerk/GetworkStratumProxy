using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace GetworkStratumProxy.Stratum
{
    public class StratumListener
    {
        public bool IsRunning { get; internal set; }
        public TcpListener TcpListener { get; private set; }
        public ConcurrentDictionary<EndPoint, StratumClient> StratumClients { get; private set; }

        public StratumListener(IPAddress address, int port)
        {
            TcpListener = new TcpListener(address, port);
            StratumClients = new ConcurrentDictionary<EndPoint, StratumClient>();
        }
    }
}
