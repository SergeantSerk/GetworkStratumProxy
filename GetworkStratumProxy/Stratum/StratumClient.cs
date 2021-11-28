using System;
using System.IO;
using System.Net.Sockets;

namespace GetworkStratumProxy
{
    public enum StratumState
    {
        Unknown,
        Authorised,
        Subscribed
    }

    public class StratumClient : IDisposable
    {
        private bool disposedValue;

        public TcpClient TcpClient { get; private set; }
        public StreamReader StreamReader { get; private set; }
        public StreamWriter StreamWriter { get; private set; }

        public bool MiningReady { get; set; }
        public string[] PreviousWork { get; set; } = null;

        public StratumClient(TcpClient tcpClient)
        {
            TcpClient = tcpClient;
            var networkStream = TcpClient.GetStream();
            StreamReader = new StreamReader(networkStream);
            StreamWriter = new StreamWriter(networkStream);
        }

        public bool IsSameWork(string[] currentWork)
        {
            if (PreviousWork.Length != currentWork.Length)
            {
                return false;
            }

            for (int i = 0; i < PreviousWork.Length; ++i)
            {
                if (PreviousWork[i] != currentWork[i])
                {
                    return false;
                }
            }

            return true;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
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
