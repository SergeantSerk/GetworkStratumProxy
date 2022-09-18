using GetworkStratumProxy.Extension;
using GetworkStratumProxy.Rpc;
using GetworkStratumProxy.Rpc.Nicehash;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.Mining;
using StreamJsonRpc;
using System;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace GetworkStratumProxy.Proxy.Client
{
    public sealed class NicehashProxyClient : BaseProxyClient
    {
        public const string ProtocolVersion = "EthereumStratum/1.0.0";

        private IEthGetWork GetWorkService { get; set; }
        private IEthSubmitWork SubmitWorkService { get; set; }

        private int CurrentJobId { get; set; } = 0;
        private bool XNSub { get; set; } = false;
        private bool CanAcceptJob { get; set; } = false;

        private decimal PreviousDifficulty { get; set; } = -1;

        public NicehashProxyClient(TcpClient tcpClient, IEthGetWork getWorkService, IEthSubmitWork submitWorkService) : base(tcpClient)
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
            using var networkStream = TcpClient.GetStream();
            using var formatter = new JsonMessageFormatter { ProtocolVersion = new Version(1, 0) };
            using var handler = new NewLineDelimitedMessageHandler(networkStream, networkStream, formatter);
            using var jsonRpc = new JsonRpc(handler, this);

            jsonRpc.StartListening();
            await jsonRpc.Completion;
            ConsoleHelper.Log(GetType().Name, $"RPC service stopped for {Endpoint}", LogLevel.Debug);
        }

        internal void NewJobNotificationEvent(object sender, EthWork e)
        {
            if (StratumState.HasFlag(StratumState.Authorised) && StratumState.HasFlag(StratumState.Subscribed))
            {
                // e[] = { headerHash, seedHash, Target }
                string headerHash = e.Header.HexValue;
                string seedHash = e.Seed.HexValue;
                string target = e.Target.HexValue;
                bool clearJobQueue = true;

                decimal difficulty = Constants.GetDifficultyFromTarget(new HexBigInteger(target));
                if (PreviousDifficulty == -1 || difficulty != PreviousDifficulty)
                {
                    var setDifficultyNotification = new SetDifficultyNotification(difficulty);
                    ConsoleHelper.Log(GetType().Name, $"Setting mining difficulty " +
                        $"({target}) to {Endpoint}", LogLevel.Information);
                    Notify(setDifficultyNotification);
                    PreviousDifficulty = difficulty;
                }

                var miningNotifyNotification = new MiningNotifyNotification(CurrentJobId++, seedHash, headerHash, clearJobQueue);
                ConsoleHelper.Log(GetType().Name, $"Sending job " +
                    $"({headerHash[..Constants.WorkHeaderCharactersPrefixCount]}...) to {Endpoint}", LogLevel.Information);
                Notify(miningNotifyNotification);
            }
        }

        [JsonRpcMethod("mining.subscribe")]
        public object[] Subscribe(string minerName, string protocol)
        {
            ConsoleHelper.Log(GetType().Name, $"Miner {Endpoint} subscribe ({minerName}-{protocol})", LogLevel.Debug);
            if (protocol != ProtocolVersion)
            {
                throw new Exception($"Unsupported protocol \"{protocol}\"");
            }

            // No login handler therefore always successful
            StratumState |= StratumState.Subscribed;
            ConsoleHelper.Log(GetType().Name, $"Miner {Endpoint} successfully subscribed", LogLevel.Information);

            byte[] connectionIdBytes = new byte[16];
            RandomNumberGenerator.Fill(connectionIdBytes);
            string connectionId = Convert.ToHexString(connectionIdBytes);

            string[] miningParams = new string[] { "mining.notify", connectionId, ProtocolVersion };
            string extraNonce = ""; // Let the client infer their own nonce

            object[] response = new object[] { miningParams, extraNonce };
            ConsoleHelper.Log(GetType().Name, $"Sending subscribe response to {Endpoint}", LogLevel.Debug);
            return response;
        }

        [JsonRpcMethod("mining.authorize")]
        public bool Authorise(string username, string password)
        {
            ConsoleHelper.Log(GetType().Name, $"Miner {Endpoint} login ({username}:{password})", LogLevel.Debug);

            // No login handler therefore always successful
            StratumState |= StratumState.Authorised;
            ConsoleHelper.Log(GetType().Name, $"Miner {Endpoint} logged in successfully", LogLevel.Information);

            ConsoleHelper.Log(GetType().Name, $"Sending login response to {Endpoint}", LogLevel.Debug);
            return StratumState.HasFlag(StratumState.Authorised) &&
                StratumState.HasFlag(StratumState.Subscribed);
        }

        [JsonRpcMethod("mining.extranonce.subscribe")]
        public bool ExtraNonceSubscribe()
        {
            ConsoleHelper.Log(GetType().Name, $"Miner {Endpoint} requested XNSUB", LogLevel.Debug);
            XNSub = true;
            return true;
        }

        public override void Dispose()
        {
            TcpClient.Dispose();
        }
    }
}
