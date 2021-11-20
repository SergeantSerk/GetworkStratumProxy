using CommandLine;
using GetworkStratumProxy.ConsoleApp.Extension;
using GetworkStratumProxy.Extension;
using GetworkStratumProxy.JsonRpc;
using GetworkStratumProxy.JsonRpc.Eth;
using Nethereum.Geth;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;

namespace GetworkStratumProxy.ConsoleApp
{
    public class Program
    {
        public static bool IsRunning { get; set; } = true;
        public static bool IsEnded { get; private set; } = false;
        public static IWeb3Geth Geth { get; private set; }
        private static TcpListener StratumListener { get; set; }
        public static ConcurrentDictionary<EndPoint, StratumClient> StratumClients { get; } = new ConcurrentDictionary<EndPoint, StratumClient>();

        public static async Task Main(string[] args)
        {
            await Parser.Default.ParseArguments<CommandLineOptions>(args)
                .WithParsedAsync(OptionParseOkAsync);
        }

        private static async Task OptionParseOkAsync(CommandLineOptions options)
        {
            ConsoleHelper.IsVerbose = options.Verbose;

            Geth = new Web3Geth(options.RpcUri.AbsoluteUri);
            StratumListener = new TcpListener(options.StratumIPAddress, options.StratumPort);

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

            ConsoleHelper.Log("Stratum", $"Listening on {options.StratumIPAddress}:{options.StratumPort}", LogLevel.Information);
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
                if (!StratumClients.TryGetValue(client.Client.RemoteEndPoint, out StratumClient stratumClient))
                {
                    // Remote endpoint not registered, add new client
                    ConsoleHelper.Log(client.Client.RemoteEndPoint, "Registered new client", LogLevel.Information);
                    stratumClient = new StratumClient(client);
                    StratumClients.TryAdd(client.Client.RemoteEndPoint, stratumClient);
                }

                await ProcessStratumClientAsync(stratumClient);
            }
        }

        private static async Task SendJobToClientAsync(StratumClient stratumClient)
        {
            if (stratumClient.MiningReady)
            {
                string[] workParams = await Geth.Eth.Mining.GetWork.SendRequestAsync();
                if (stratumClient.PreviousWork == null || !stratumClient.IsSameWork(workParams))
                {
                    stratumClient.PreviousWork = workParams;
                    ConsoleHelper.Log(stratumClient.TcpClient.Client.RemoteEndPoint, "Building fresh getWork from node", LogLevel.Information);
                    var getworkResponse = new BaseResponse<string[]>
                    {
                        Id = 0,
                        JsonRpc = "2.0",
                        Error = null,
                        Result = workParams
                    };

                    string responseContentJson = JsonSerializer.Serialize(getworkResponse);
                    ConsoleHelper.Log(stratumClient.TcpClient.Client.RemoteEndPoint, "Sending job", LogLevel.Information);
                    ConsoleHelper.Log(stratumClient.TcpClient.Client.RemoteEndPoint, $"(O) {responseContentJson}", LogLevel.Debug);
                    await stratumClient.StreamWriter.WriteLineAsync($"{responseContentJson}");
                    await stratumClient.StreamWriter.FlushAsync();
                }
            }
            else
            {
                await Task.CompletedTask;
            }
        }

        private static async Task ProcessStratumClientAsync(StratumClient stratumClient)
        {
            EndPoint endpoint = stratumClient.TcpClient.Client.RemoteEndPoint;
            bool validRequestResponse = true;

            TcpState state;
            bool disconnected;
            do
            {
                state = stratumClient.TcpClient.GetState();
                disconnected =
                    state == TcpState.Closing ||
                    state == TcpState.CloseWait ||
                    state == TcpState.Closed;

                if (!disconnected)
                {
                    string requestContent = await stratumClient.StreamReader.ReadLineAsync();
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
                            stratumClient.MiningReady = true;
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
                            ConsoleHelper.Log(endpoint, $"Solution found was {(workAccepted ? "accepted" : "rejected")}", workAccepted ? LogLevel.Success : LogLevel.Error);

                            responseContent = JsonSerializer.Serialize(submitWorkResponse);
                            ConsoleHelper.Log(endpoint, "Acknowledging submitted work", LogLevel.Information);
                        }

                        ConsoleHelper.Log(endpoint, $"(O) {responseContent}", LogLevel.Debug);
                        await stratumClient.StreamWriter.WriteLineAsync($"{responseContent}");
                        await stratumClient.StreamWriter.FlushAsync();
                    }
                }
            } while (validRequestResponse && !disconnected);

            if (StratumClients.TryRemove(endpoint, out StratumClient stratumClientToFinalise))
            {
                stratumClientToFinalise.Dispose();
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
