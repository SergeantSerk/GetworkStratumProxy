using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace GetworkStratumProxy.Extension
{
    public static class TcpClientExtensions
    {
        public static TcpState GetState(this TcpClient tcpClient)
        {
            var tcpConnection = IPGlobalProperties.GetIPGlobalProperties()
              .GetActiveTcpConnections()
              .SingleOrDefault(_ => _.LocalEndPoint.Equals(tcpClient.Client.LocalEndPoint)
                                 && _.RemoteEndPoint.Equals(tcpClient.Client.RemoteEndPoint));

            return tcpConnection != null ? tcpConnection.State : TcpState.Unknown;
        }

        public static bool IsDisconnected(this TcpClient tcpClient)
        {
            TcpState state = tcpClient.GetState();
            return state == TcpState.Closing || state == TcpState.CloseWait || state == TcpState.Closed;
        }
    }
}
