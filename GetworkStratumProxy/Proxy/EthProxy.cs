using GetworkStratumProxy.Extension;
using GetworkStratumProxy.Node;
using GetworkStratumProxy.Rpc;
using StreamJsonRpc;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace GetworkStratumProxy.Proxy
{
    public sealed class EthProxy : BaseProxy
    {
        public override bool IsListening { get; protected set; }
        protected override TcpListener Server { get; set; }

        public EthProxy(BaseNode node, IPAddress address, int port) : base(node)
        {
            Server = new TcpListener(address, port);
        }

        protected override async void HandleTcpClient(IAsyncResult ar)
        {
            TcpClient client;
            try
            {
                TcpListener listener = ar.AsyncState as TcpListener;
                client = listener.EndAcceptTcpClient(ar);
            }
            catch (ObjectDisposedException)
            {
                // Safely ignore disposed connections
                ConsoleHelper.Log(GetType().Name, "Could not accept connected client, disposing", LogLevel.Warning);
                return;
            }

            var endpoint = client.Client.RemoteEndPoint;
            ConsoleHelper.Log(GetType().Name, $"{endpoint} connected", LogLevel.Information);
            if (!Clients.TryGetValue(endpoint, out StratumClient stratumClient))
            {
                // Remote endpoint not registered, add new client
                ConsoleHelper.Log(GetType().Name, $"Registered new client {endpoint}", LogLevel.Debug);
                stratumClient = new StratumClient(client);
                Clients.TryAdd(endpoint, stratumClient);
            }

            var ethProxyClientRpc = new EthProxyClientRpc(endpoint, Node.Web3.Eth.Mining.GetWork, Node.Web3.Eth.Mining.SubmitWork);
            using (NetworkStream networkStream = client.GetStream())
            using (var formatter = new JsonMessageFormatter { ProtocolVersion = new Version(1, 0) })
            using (var handler = new NewLineDelimitedMessageHandler(networkStream, networkStream, formatter))
            using (var jsonRpc = new JsonRpc(handler, ethProxyClientRpc))
            {
                bool connected = true;
                jsonRpc.StartListening();

                EventHandler<string[]> NewJobHandler = null;
                Node.NewJobReceived += NewJobHandler = (o, e) =>
                {
                    // Check if client is disconnected
                    if (!connected)
                    {
                        // Unsubscribe event
                        Node.NewJobReceived -= NewJobHandler;
                        ConsoleHelper.Log(GetType().Name, $"Client {endpoint} unsubscribed from jobs", LogLevel.Information);
                        return;
                    }

                    if (ethProxyClientRpc.StratumState == StratumState.Subscribed)
                    {
                        var notifyJobResponse = new Rpc.EthProxy.NotifyJobResponse(e);
                        var notifyJobResponseString = JsonSerializer.Serialize(notifyJobResponse);
                        ConsoleHelper.Log(GetType().Name, $"Sending job " +
                            $"({e[0][..Constants.JobCharactersPrefixCount]}...) to {endpoint}", LogLevel.Information);
                        ConsoleHelper.Log(GetType().Name, $"(O) {notifyJobResponseString} -> {endpoint}", LogLevel.Debug);
                        stratumClient.StreamWriter.WriteLine(notifyJobResponseString);
                        stratumClient.StreamWriter.Flush();
                    }
                };

                await jsonRpc.Completion;
                connected = false;
            }

            if (Clients.TryRemove(endpoint, out StratumClient stratumClientToFinalise))
            {
                stratumClientToFinalise.Dispose();
                ConsoleHelper.Log(GetType().Name, $"{endpoint} disconnected", LogLevel.Information);
            }
        }
    }
}
