using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace GetworkStratumProxy.ConsoleApp.Extension
{
    internal static class TcpClientExtensions
    {
        public static TcpState GetState(this TcpClient tcpClient)
        {
            var tcpConnection = IPGlobalProperties.GetIPGlobalProperties()
              .GetActiveTcpConnections()
              .SingleOrDefault(_ => _.LocalEndPoint.Equals(tcpClient.Client.LocalEndPoint)
                                 && _.RemoteEndPoint.Equals(tcpClient.Client.RemoteEndPoint));

            return tcpConnection != null ? tcpConnection.State : TcpState.Unknown;
        }
    }
}
