using GetworkStratumProxy.Extension;
using GetworkStratumProxy.Rpc.Eth;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace GetworkStratumProxy.Proxy.Eth.Client
{
    public enum StratumState
    {
        Unauthorised,
        Authorised,
        Subscribed
    }

    public abstract class BaseEthProxyClient : IDisposable
    {
        public TcpClient TcpClient { get; protected set; }
        public EndPoint Endpoint { get; private set; }
        public StratumState StratumState { get; protected set; }

        protected StreamWriter BackgroundWorkWriter { get; set; }

        public BaseEthProxyClient(TcpClient tcpClient)
        {
            TcpClient = tcpClient;
            Endpoint = tcpClient.Client.RemoteEndPoint;
            StratumState = StratumState.Unauthorised;
        }

        protected void Notify<T>(T notification) where T : JsonRpcResponse
        {
            var notificationString = JsonSerializer.Serialize(notification);
            ConsoleHelper.Log(GetType().Name, $"(O) {notificationString} -> {Endpoint}", LogLevel.Debug);
            BackgroundWorkWriter.WriteLine(notificationString);
            BackgroundWorkWriter.Flush();
        }

        public abstract void Dispose();
    }
}
