using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GetworkStratumProxy.Network
{
    internal class PeekableNewLineDelimitedStream : Stream, IDisposable
    {
        private NetworkStream NetworkStream { get; }
        private MemoryStream ReadStreamBuffer { get; }

        public override bool CanRead => NetworkStream.CanRead;
        public override bool CanSeek => NetworkStream.CanSeek;
        public override bool CanWrite => NetworkStream.CanWrite;
        public override long Length => NetworkStream.Length;
        public override long Position
        {
            get => NetworkStream.Position;
            set => NetworkStream.Position = value;
        }

        public PeekableNewLineDelimitedStream(NetworkStream networkStream)
        {
            NetworkStream = networkStream;
            ReadStreamBuffer = new MemoryStream();
        }

        public override void Flush()
        {
            NetworkStream.Flush();
        }

        public new int ReadByte()
        {
            return ReadStreamBuffer.Position == ReadStreamBuffer.Length ? base.ReadByte() : ReadStreamBuffer.ReadByte();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = 0;
            // If any line of data exists in memory
            if (ReadStreamBuffer.Position != ReadStreamBuffer.Length)
            {
                int current;
                while (bytesRead < count && (current = ReadStreamBuffer.ReadByte()) != -1)
                {
                    if (bytesRead >= buffer.Length)
                    {
                        break;
                    }

                    buffer[offset + bytesRead] = (byte)current;
                    bytesRead++;
                }

                // Clear internal buffer
                ReadStreamBuffer.SetLength(0);
            }
            else
            {
                // ReadStreamBuffer did not fill buffer with enough data
                // read more
                byte[] networkBytes = new byte[count];
                bytesRead = NetworkStream.Read(networkBytes, offset, count);
                for (int i = 0; i < networkBytes.Length; i++)
                {
                    buffer[offset + i] = networkBytes[i];
                }
            }

            return bytesRead;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            return Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return NetworkStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            NetworkStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            NetworkStream.Write(buffer, offset, count);
        }

        public string PeekLine()
        {
            long readStreamBufferPosition = ReadStreamBuffer.Position;
            int current;
            while ((current = ReadByte()) != '\n')
            {
                ReadStreamBuffer.WriteByte((byte)current);
            }
            // Add line terminator
            ReadStreamBuffer.WriteByte((byte)'\n');
            ReadStreamBuffer.Seek(readStreamBufferPosition, SeekOrigin.Begin);

            byte[] bytes = ReadStreamBuffer.ToArray();
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
