using GetworkStratumProxy.Extension;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.Mining;
using StreamJsonRpc;
using System;
using System.Net;
using System.Threading.Tasks;

namespace GetworkStratumProxy.Rpc
{
    public class EthProxyClientRpc
    {
        private IEthGetWork GetWorkService { get; set; }
        private IEthSubmitWork SubmitWorkService { get; set; }

        public EndPoint Endpoint { get; private set; }
        public StratumState StratumState { get; private set; }
        public string[] CurrentJob { get; private set; }

        public EthProxyClientRpc(EndPoint endpoint, IEthGetWork getWorkService, IEthSubmitWork submitWorkService)
        {
            Endpoint = endpoint;
            StratumState = StratumState.Unauthorised;
            GetWorkService = getWorkService;
            SubmitWorkService = submitWorkService;
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
    }
}
