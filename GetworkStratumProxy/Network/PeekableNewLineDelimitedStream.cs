using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GetworkStratumProxy.Network
{
    internal class PeekableNewLineDelimitedStream : NetworkStream
    {
        private MemoryStream ReadStreamBuffer { get; }

        public PeekableNewLineDelimitedStream(Socket socket) : base(socket)
        {
            ReadStreamBuffer = new MemoryStream();
        }

        public override int ReadByte()
        {
            byte[] buffer = new byte[1];
            Read(buffer, 0, buffer.Length);
            return buffer[0];
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
                bytesRead = base.Read(networkBytes, offset, count);
                for (int i = 0; i < networkBytes.Length; i++)
                {
                    buffer[offset + i] = networkBytes[i];
                }
            }

            return bytesRead;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return await Task.Run(() => Read(buffer, offset, count), cancellationToken);
        }

        public override int Read(Span<byte> buffer)
        {
            byte[] internalBuffer = new byte[buffer.Length];
            int bytesRead = Read(internalBuffer, 0, internalBuffer.Length);
            internalBuffer.CopyTo(buffer);
            return bytesRead;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => Read(buffer.Span), cancellationToken);
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int size, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
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
            string line = Encoding.UTF8.GetString(bytes);
            Console.WriteLine(line);
            return line;
        }
    }
}
