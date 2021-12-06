using GetworkStratumProxy.Extension;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.Mining;
using StreamJsonRpc;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;

namespace GetworkStratumProxy.Proxy.Client
{
    public sealed class EthProxyClient : BaseProxyClient
    {
        private IEthGetWork GetWorkService { get; set; }
        private IEthSubmitWork SubmitWorkService { get; set; }

        public StreamWriter BackgroundJobWriter { get; private set; }
        public string[] CurrentJob { get; internal set; }

        public EthProxyClient(TcpClient tcpClient, IEthGetWork getWorkService, IEthSubmitWork submitWorkService) : base(tcpClient)
        {
            var networkStream = TcpClient.GetStream();
            BackgroundJobWriter = new StreamWriter(networkStream);

            GetWorkService = getWorkService;
            SubmitWorkService = submitWorkService;
        }

        internal bool IsSameJob(string[] job)
        {
            if (CurrentJob.Length != job.Length)
            {
                return false;
            }

            for (int i = 0; i < CurrentJob.Length; ++i)
            {
                if (CurrentJob[i] != job[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Blocking listen and respond to EthProxy RPC messages.
        /// </summary>
        internal async Task StartListeningAsync()
        {
            using var networkStream = TcpClient.GetStream();
            using var formatter = new JsonMessageFormatter { ProtocolVersion = new Version(1, 0) };
            using var handler = new NewLineDelimitedMessageHandler(networkStream, networkStream, formatter);
            using var jsonRpc = new JsonRpc(handler, this);

            jsonRpc.StartListening();
            await jsonRpc.Completion;
            ConsoleHelper.Log(GetType().Name, $"RPC service stopped for {Endpoint}", LogLevel.Debug);
        }

        internal void NewJobNotificationEvent(object sender, string[] e)
        {
            if (StratumState == StratumState.Subscribed && !IsSameJob(e))
            {
                CurrentJob = e;
                var notifyJobResponse = new Rpc.EthProxy.NotifyJobResponse(e);
                var notifyJobResponseString = JsonSerializer.Serialize(notifyJobResponse);

                ConsoleHelper.Log(GetType().Name, $"Sending job " +
                    $"({e[0][..Constants.JobCharactersPrefixCount]}...) to {Endpoint}", LogLevel.Information);
                ConsoleHelper.Log(GetType().Name, $"(O) {notifyJobResponseString} -> {Endpoint}", LogLevel.Debug);
                BackgroundJobWriter.WriteLine(notifyJobResponseString);
                BackgroundJobWriter.Flush();
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

            string[] job = await GetWorkService.SendRequestAsync();
            CurrentJob = job;

            string headerHash = job[0];
            ConsoleHelper.Log(GetType().Name, $"Sending latest job " +
                $"({headerHash[..Constants.JobCharactersPrefixCount]}...) for {Endpoint}", LogLevel.Information);

            ConsoleHelper.Log(GetType().Name, $"Sending job to {Endpoint}", LogLevel.Debug);
            return job;
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
            BackgroundJobWriter.Dispose();
            TcpClient.Dispose();
        }
    }
}
