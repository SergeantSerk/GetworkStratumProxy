using CommandLine;
using GetworkStratumProxy.ConsoleApp.Extension;
using GetworkStratumProxy.ConsoleApp.JsonRpc;
using GetworkStratumProxy.ConsoleApp.JsonRpc.Eth;
using Nethereum.Geth;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;

namespace GetworkStratumProxy.ConsoleApp
{
    public class Options
    {
        [Option('v', "verbose", Required = false, HelpText = "Show more detailed and verbose output.")]
        public bool Verbose { get; set; }

        [Option('r', "rpc", Required = true, HelpText = "RPC endpoint URI for the node to getWork from, such as http://127.0.0.1:8545/")]
        public Uri RpcUri { get; set; }

        [Option('a', "address", Required = false, HelpText = "IP address to listen stratum requests from e.g. 0.0.0.0")]
        public IPAddress IPAddress { get; set; }

        [Option('p', "port", Required = false, Default = 3131, HelpText = "Port number to listen stratum requests from e.g. 3131")]
        public int Port { get; set; }
    }

    public class Program
    {
        public static bool IsRunning { get; set; } = true;
        public static bool IsEnded { get; private set; } = false;
        public static IWeb3Geth Geth { get; private set; }
        private static TcpListener StratumListener { get; set; }
        public static ConcurrentDictionary<EndPoint, StreamHandler> StratumClients { get; } = new ConcurrentDictionary<EndPoint, StreamHandler>();

        public static async Task Main(string[] args)
        {
            await Parser.Default.ParseArguments<Options>(args)
                .WithParsedAsync(OptionParseOkAsync);
        }

        private static async Task OptionParseOkAsync(Options options)
        {
            // If null, set to any IP listen
            options.IPAddress ??= IPAddress.Any;

            ConsoleHelper.IsVerbose = options.Verbose;

            Geth = new Web3Geth(options.RpcUri.AbsoluteUri);
            StratumListener = new TcpListener(options.IPAddress, options.Port);

            ConsoleHelper.Log("Timer", "Initialising getwork timer and handler", LogLevel.Information);
            int sleepPeriod = 500;
            new Timer(sleepPeriod) { AutoReset = true, Enabled = true }.Elapsed += async (o, e) =>
            {
                if (!IsRunning)
                {
                    ConsoleHelper.Log("Timer", "Disposing getWork loop", LogLevel.Information);
                    (o as Timer).Dispose();
                }
                else if (!StratumClients.IsEmpty)
                {
                    foreach (var stratumClient in StratumClients)
                    {
                        if (stratumClient.Value.TcpClient.IsDisconnected())
                        {
                            if (StratumClients.TryRemove(stratumClient))
                            {
                                stratumClient.Value.Dispose();
                                ConsoleHelper.Log(stratumClient.Key, $"Client disconnected", LogLevel.Information);
                            }
                        }
                        else
                        {
                            // Broadcast received work params to connected stratum clients
                            if (stratumClient.Value.MiningReady)
                            {
                                await SendJobToClientAsync(stratumClient.Value);
                            }
                        }
                    }

                    await Task.Delay(sleepPeriod);
                }
            };

            ConsoleHelper.Log("Stratum", $"Listening on {options.IPAddress}:{options.Port}", LogLevel.Information);
            StratumListener.Start();

            Console.CancelKeyPress += (o, e) =>
            {
                e.Cancel = true;
                ConsoleHelper.Log("Internal", $"Caught {e.SpecialKey}, stopping", LogLevel.Information);
                IsRunning = false;
                Stop();
            };

            ConsoleHelper.Log("Stratum", "Ready and waiting for new stratum clients", LogLevel.Information);
            while (IsRunning)
            {
                StratumListener
                    .BeginAcceptTcpClient(BeginAcceptStratumClientAsync, StratumListener)
                    .AsyncWaitHandle
                    .WaitOne(); // Do not infinitely spawn listen threads
            }

            while (!IsEnded)
            {
                await Task.Delay(1000);
            }

            ConsoleHelper.Log("Shutdown", "Exited gracefully", LogLevel.Information);
        }

        private static async void BeginAcceptStratumClientAsync(IAsyncResult ar)
        {
            TcpClient client = null;
            try
            {
                TcpListener listener = ar.AsyncState as TcpListener;
                client = listener.EndAcceptTcpClient(ar);
            }
            catch (ObjectDisposedException)
            {
                // Safely ignore disposed connections
                ConsoleHelper.Log("Listener", "Could not accept connected, client was disposed", LogLevel.Warning);
            }

            if (client != null && !StratumClients.ContainsKey(client.Client.RemoteEndPoint))
            {
                ConsoleHelper.Log(client.Client.RemoteEndPoint, "Connected", LogLevel.Information);
                NetworkStream networkStream = client.GetStream();

                if (!StratumClients.TryGetValue(client.Client.RemoteEndPoint, out StreamHandler streamHandler))
                {
                    // Add StreamHandler to ConcurrentDictionary, if it does not exist
                    var streamReader = new StreamReader(networkStream);
                    var streamWriter = new StreamWriter(networkStream);
                    streamHandler = new StreamHandler(client, streamReader, streamWriter);
                    StratumClients.TryAdd(client.Client.RemoteEndPoint, streamHandler);
                }

                await ProcessStratumClientAsync(streamHandler);
            }
        }

        private static async Task SendJobToClientAsync(StreamHandler streamHandler)
        {
            if (streamHandler.MiningReady)
            {
                string[] workParams = await Geth.Eth.Mining.GetWork.SendRequestAsync();
                if (streamHandler.PreviousWork == null || !IsWorkSame(streamHandler.PreviousWork, workParams))
                {
                    streamHandler.PreviousWork = workParams;
                    ConsoleHelper.Log(streamHandler.TcpClient.Client.RemoteEndPoint, "Building fresh getWork from node", LogLevel.Information);
                    var getworkResponse = new BaseResponse<string[]>
                    {
                        Id = 0,
                        JsonRpc = "2.0",
                        Error = null,
                        Result = workParams
                    };

                    string responseContentJson = JsonSerializer.Serialize(getworkResponse);
                    ConsoleHelper.Log(streamHandler.TcpClient.Client.RemoteEndPoint, "Sending job", LogLevel.Information);
                    ConsoleHelper.Log(streamHandler.TcpClient.Client.RemoteEndPoint, $"(O) {responseContentJson}", LogLevel.Debug);
                    await streamHandler.StreamWriter.WriteLineAsync($"{responseContentJson}");
                    await streamHandler.StreamWriter.FlushAsync();
                }
            }
            else
            {
                await Task.CompletedTask;
            }
        }

        private static bool IsWorkSame(string[] previousWork, string[] currentWork)
        {
            if (previousWork.Length != currentWork.Length)
            {
                return false;
            }

            for (int i = 0; i < previousWork.Length; ++i)
            {
                if (previousWork[i] != currentWork[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static async Task ProcessStratumClientAsync(StreamHandler streamHandler)
        {
            EndPoint endpoint = streamHandler.TcpClient.Client.RemoteEndPoint;
            bool validRequestResponse = true;

            TcpState state;
            bool disconnected;
            do
            {
                state = streamHandler.TcpClient.GetState();
                disconnected =
                    state == TcpState.Closing ||
                    state == TcpState.CloseWait ||
                    state == TcpState.Closed;

                if (!disconnected)
                {
                    string requestContent = await streamHandler.StreamReader.ReadLineAsync();
                    if (!string.IsNullOrWhiteSpace(requestContent))
                    {
                        ConsoleHelper.Log(endpoint, $"(I) {requestContent}", LogLevel.Debug);
                        BaseRequest<object> baseRequest = JsonSerializer.Deserialize<BaseRequest<object>>(requestContent);

                        string responseContent = null;
                        if (baseRequest.Method == "eth_submitLogin")
                        {
                            ConsoleHelper.Log(endpoint, "Miner login", LogLevel.Information);
                            var loginRequest = JsonSerializer.Deserialize<EthSubmitLoginRequest>(requestContent);

                            ConsoleHelper.Log(endpoint, "Miner login successful", LogLevel.Information);
                            var loginResponse = new EthSubmitLoginResponse(loginRequest, true);
                            responseContent = JsonSerializer.Serialize(loginResponse);

                            ConsoleHelper.Log(endpoint, "Sending login response", LogLevel.Information);
                            streamHandler.MiningReady = true;
                        }
                        else if (baseRequest.Method == "eth_getWork")
                        {
                            ConsoleHelper.Log(endpoint, "Miner getwork", LogLevel.Information);
                            var getworkRequest = JsonSerializer.Deserialize<BaseRequest<object[]>>(requestContent);

                            ConsoleHelper.Log("RPC", "Polling getwork from node", LogLevel.Information);
                            string[] workParams = await Geth.Eth.Mining.GetWork.SendRequestAsync();
                            var getworkResponse = new BaseResponse<string[]>
                            {
                                Id = getworkRequest.Id,
                                JsonRpc = "2.0",
                                Error = null,
                                Result = workParams
                            };

                            responseContent = JsonSerializer.Serialize(getworkResponse);
                            ConsoleHelper.Log(endpoint, "Sending job", LogLevel.Information);
                        }
                        else if (baseRequest.Method == "eth_submitHashrate")
                        {
                            ConsoleHelper.Log(endpoint, "Miner hashrate submit", LogLevel.Information);
                            var submitHashrateRequest = JsonSerializer.Deserialize<BaseRequest<string[]>>(requestContent);
                            var submitHashrateResponse = new BaseResponse<bool>
                            {
                                Id = submitHashrateRequest.Id,
                                JsonRpc = "2.0",
                                Error = null,
                                Result = true
                            };

                            responseContent = JsonSerializer.Serialize(submitHashrateResponse);
                            ConsoleHelper.Log(endpoint, "Acknowledging submitted hashrate", LogLevel.Information);
                        }
                        else if (baseRequest.Method == "eth_submitWork")
                        {
                            ConsoleHelper.Log(endpoint, "Miner submit work", LogLevel.Information);
                            var submitWorkRequest = JsonSerializer.Deserialize<BaseRequest<string[]>>(requestContent);

                            string nonce = submitWorkRequest.Params[0], header = submitWorkRequest.Params[1], mix = submitWorkRequest.Params[2];
                            bool workAccepted = await Geth.Eth.Mining.SubmitWork.SendRequestAsync(nonce, header, mix);

                            var submitWorkResponse = new BaseResponse<bool>
                            {
                                Id = submitWorkRequest.Id,
                                JsonRpc = "2.0",
                                Error = null,
                                Result = workAccepted
                            };

                            responseContent = JsonSerializer.Serialize(submitWorkResponse);
                            ConsoleHelper.Log(endpoint, "Acknowledging submitted hashrate", LogLevel.Information);
                        }

                        ConsoleHelper.Log(endpoint, $"(O) {responseContent}", LogLevel.Debug);
                        await streamHandler.StreamWriter.WriteLineAsync($"{responseContent}");
                        await streamHandler.StreamWriter.FlushAsync();
                    }
                }
            } while (validRequestResponse && !disconnected);

            if (StratumClients.TryRemove(endpoint, out StreamHandler streamHandlerToFinalise))
            {
                streamHandlerToFinalise.Dispose();
                ConsoleHelper.Log(endpoint, $"Client disconnected", LogLevel.Information);
            }
        }

        private static void Stop()
        {
            ConsoleHelper.Log("Shutdown", "Shutting down stratum server", LogLevel.Information);
            StratumListener.Stop();
            foreach (var stratumClient in StratumClients)
            {
                stratumClient.Value.Dispose();
                ConsoleHelper.Log("Shutdown", $"Disconnecting client {stratumClient.Key}", LogLevel.Information);
            }

            IsEnded = true;
        }
    }
}
