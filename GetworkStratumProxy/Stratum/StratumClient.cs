using System;
using System.IO;
using System.Net.Sockets;

namespace GetworkStratumProxy
{
    public class StratumClient : IDisposable
    {
        private bool disposedValue;

        public TcpClient TcpClient { get; set; }
        public StreamReader StreamReader { get; set; }
        public StreamWriter StreamWriter { get; set; }

        public bool MiningReady { get; set; }
        public string[] PreviousWork { get; set; } = null;

        public StratumClient(TcpClient tcpClient, StreamReader streamReader, StreamWriter streamWriter)
        {
            TcpClient = tcpClient;
            StreamReader = streamReader;
            StreamWriter = streamWriter;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    StreamWriter.Dispose();
                    StreamReader.Dispose();
                    TcpClient.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
