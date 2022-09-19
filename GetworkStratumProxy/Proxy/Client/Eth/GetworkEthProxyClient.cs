using GetworkStratumProxy.Extension;
using GetworkStratumProxy.Network;
using GetworkStratumProxy.Node.Eth;
using GetworkStratumProxy.Rpc;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.Mining;
using StreamJsonRpc;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;

namespace GetworkStratumProxy.Proxy.Client.Eth
{
    public sealed class EthProxyClient : BaseEthProxyClient
    {
        private IEthGetWork GetWorkService { get; set; }
        private IEthSubmitWork SubmitWorkService { get; set; }

        public EthWork CurrentEthWork { get; internal set; }

        public EthProxyClient(TcpClient tcpClient, IEthGetWork getWorkService, IEthSubmitWork submitWorkService) : base(tcpClient)
        {
            var networkStream = TcpClient.GetStream();
            BackgroundWorkWriter = new StreamWriter(networkStream);

            GetWorkService = getWorkService;
            SubmitWorkService = submitWorkService;
        }

        /// <summary>
        /// Blocking listen and respond to EthProxy RPC messages.
        /// </summary>
        internal async Task StartListeningAsync()
        {
            using var peekableStream = new PeekableNewLineDelimitedStream(TcpClient.GetStream().Socket);
            var message = JsonSerializer.Deserialize<JsonRpcRequest>(peekableStream.PeekLine());

            using var formatter = new JsonMessageFormatter { ProtocolVersion = Version.Parse(message.JsonRpc ?? "1.0") };
            using var handler = new NewLineDelimitedMessageHandler(peekableStream, peekableStream, formatter);
            using var jsonRpc = new JsonRpc(handler, this);

            jsonRpc.StartListening();

            await jsonRpc.Completion;
            ConsoleHelper.Log(GetType().Name, $"RPC service stopped for {Endpoint}", LogLevel.Debug);
        }

        internal void NewWorkNotificationEvent(object sender, EthWork newEthWork)
        {
            if (StratumState == StratumState.Subscribed && !CurrentEthWork.Equals(newEthWork))
            {
                CurrentEthWork = newEthWork;
                var ethWorkNotification = new Rpc.EthProxy.NewEthWorkNotification(newEthWork);
                ConsoleHelper.Log(GetType().Name, $"Sending work " +
                    $"({newEthWork.Header.HexValue[..EthashUtilities.WorkHeaderCharactersPrefixCount]}...) to {Endpoint}", LogLevel.Information);

                try
                {
                    Notify(ethWorkNotification);
                }
                catch (ObjectDisposedException)
                {
                    // Background work writer stream disposed, unsubscribe here
                    var node = sender as BaseEthNode;
                    node.NewWorkReceived -= NewWorkNotificationEvent;
                    ConsoleHelper.Log(GetType().Name, $"Client {Endpoint} unsubscribed from new work", LogLevel.Information);
                }
            }
        }

        [JsonRpcMethod("eth_submitLogin")]
        public bool Login(string username, string password)
        {
            ConsoleHelper.Log(GetType().Name, $"Miner login ({username}:{password}) from {Endpoint}", LogLevel.Debug);

            // No login handler therefore always successful
            StratumState = StratumState.Authorised;
            ConsoleHelper.Log(GetType().Name, $"Miner login successful to {Endpoint}", LogLevel.Information);

            ConsoleHelper.Log(GetType().Name, $"Sending login response to {Endpoint}", LogLevel.Debug);
            return StratumState == StratumState.Authorised;
        }

        [JsonRpcMethod("eth_getWork")]
        public async Task<string[]> GetWorkAsync()
        {
            if (StratumState == StratumState.Unauthorised)
            {
                ConsoleHelper.Log(GetType().Name, $"Miner {Endpoint} was not authorised", LogLevel.Warning);
                throw new UnauthorizedAccessException("Client is not logged in.");
            }

            ConsoleHelper.Log(GetType().Name, $"Miner getWork from {Endpoint}", LogLevel.Debug);
            StratumState = StratumState.Subscribed;

            string[] ethWorkRaw = await GetWorkService.SendRequestAsync();
            var ethWork = new EthWork(ethWorkRaw);
            CurrentEthWork = ethWork;

            ConsoleHelper.Log(GetType().Name, $"Sending latest work " +
                $"({ethWork.Header.HexValue[..EthashUtilities.WorkHeaderCharactersPrefixCount]}...) for {Endpoint}", LogLevel.Information);

            ConsoleHelper.Log(GetType().Name, $"Sending work to {Endpoint}", LogLevel.Debug);
            return ethWorkRaw;
        }

        [JsonRpcMethod("eth_submitHashrate")]
        public bool SubmitHashrate(HexBigInteger hashrate, string minerId)
        {
            ConsoleHelper.Log(GetType().Name, $"Miner hashrate submit from {Endpoint}/{minerId}", LogLevel.Debug);

            double declaredHashrateMhs = (double)hashrate.Value / Math.Pow(10, 6);
            bool result = true;

            ConsoleHelper.Log(GetType().Name, $"Acknowledging submitted hashrate ({declaredHashrateMhs} Mh/s) by {Endpoint}/{minerId}", LogLevel.Information);
            return result;
        }

        [JsonRpcMethod("eth_submitWork")]
        public async Task<bool> SubmitWorkAsync(string nonce, string header, string mix)
        {
            ConsoleHelper.Log(GetType().Name, $"Miner {Endpoint} submitted work", LogLevel.Debug);
            bool workAccepted = await SubmitWorkService.SendRequestAsync(nonce, header, mix);

            ConsoleHelper.Log(GetType().Name, $"Solution found by {Endpoint} " +
                $"was {(workAccepted ? "accepted" : "rejected")}", workAccepted ? LogLevel.Success : LogLevel.Error);

            ConsoleHelper.Log(GetType().Name, $"Acknowledging submitted work by {Endpoint}", LogLevel.Debug);
            return workAccepted;
        }

        public override void Dispose()
        {
            BackgroundWorkWriter.Dispose();
            TcpClient.Dispose();
        }
    }
}
