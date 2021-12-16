using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GetworkStratumProxy.Network
{
    internal class PeekableNewLineDelimitedStream : NetworkStream
    {
        private Encoding Encoding { get; }
        private Queue<string> BufferedLines { get; }

        public PeekableNewLineDelimitedStream(Socket socket) : this(socket, Encoding.UTF8)
        {

        }

        public PeekableNewLineDelimitedStream(Socket socket, Encoding encoding) : base(socket)
        {
            BufferedLines = new Queue<string>();
            Encoding = encoding;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead;

            // If any line of data exists in buffer
            if (BufferedLines.Count > 0)
            {
                string line = BufferedLines.Dequeue();
                byte[] lineBytes = Encoding.GetBytes(line);
                Array.Copy(lineBytes, 0, buffer, offset, Math.Min(lineBytes.Length, count));
                bytesRead = lineBytes.Length;
            }
            else
            {
                // Read straight from NetworkStream
                Console.WriteLine("yeet");
                bytesRead = base.Read(buffer, offset, count);
                Console.WriteLine("yote");
            }

            return bytesRead;
        }

        public override int Read(Span<byte> buffer)
        {
            byte[] internalBuffer = new byte[buffer.Length];
            int bytesRead = Read(internalBuffer, 0, internalBuffer.Length);
            internalBuffer.CopyTo(buffer);
            return bytesRead;
        }

        public override int ReadByte()
        {
            byte[] buffer = new byte[1];
            Read(buffer, 0, buffer.Length);
            return buffer[0];
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return await Task.Run(() => Read(buffer, offset, count), cancellationToken);
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
            using var streamReader = new StreamReader(this, leaveOpen: true);
            string line = streamReader.ReadLine() + '\n';   // Preserve newline character
            BufferedLines.Enqueue(line);

            return line;
        }
    }
}
