using System;
using System.Net;
using System.Net.Sockets;

namespace GetworkStratumProxy.Proxy.Client
{
    public enum StratumState
    {
        Unauthorised,
        Authorised,
        Subscribed
    }

    public abstract class BaseClient : IDisposable
    {
        public TcpClient TcpClient { get; protected set; }
        public EndPoint Endpoint { get; private set; }
        public StratumState StratumState { get; protected set; }

        public BaseClient(TcpClient tcpClient)
        {
            TcpClient = tcpClient;
            Endpoint = tcpClient.Client.RemoteEndPoint;
            StratumState = StratumState.Unauthorised;
        }

        public abstract void Dispose();
    }
}
