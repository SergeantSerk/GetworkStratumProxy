using CommandLine;
using GetworkStratumProxy.ConsoleApp.Extension;
using GetworkStratumProxy.Extension;
using GetworkStratumProxy.JsonRpc;
using GetworkStratumProxy.JsonRpc.Eth;
using System;
using System.Net;
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

        private static RpcStratumProxy RpcStratumProxy { get; set; }

        public static async Task Main(string[] args)
        {
            await Parser.Default.ParseArguments<CommandLineOptions>(args)
                .WithParsedAsync(OptionParseOkAsync);
        }

        private static async Task OptionParseOkAsync(CommandLineOptions options)
        {
            if (options.PollInterval <= 0)
            {
                options.PollInterval = 500;
            }

            ConsoleHelper.IsVerbose = options.Verbose;
            RpcStratumProxy = new RpcStratumProxy(options.RpcUri, options.StratumIPAddress, options.StratumPort);

            ConsoleHelper.Log("Timer", "Initialising getwork timer and handler", LogLevel.Information);
            int sleepPeriod = options.PollInterval;
            new Timer(sleepPeriod) { AutoReset = true, Enabled = true }.Elapsed += async (o, e) =>
            {
                if (!IsRunning)
                {
                    ConsoleHelper.Log("Timer", "Disposing getWork loop", LogLevel.Information);
                    (o as Timer).Dispose();
                }
                else if (!RpcStratumProxy.StratumListener.StratumClients.IsEmpty)
                {
                    foreach (var stratumClient in RpcStratumProxy.StratumListener.StratumClients)
                    {
                        if (stratumClient.Value.TcpClient.IsDisconnected())
                        {
                            if (RpcStratumProxy.StratumListener.StratumClients.TryRemove(stratumClient))
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
            RpcStratumProxy.StratumListener.Start();

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
                RpcStratumProxy
                    .StratumListener
                    .BeginAcceptTcpClient(BeginAcceptStratumClientAsync, RpcStratumProxy.StratumListener)
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

            if (client != null && !RpcStratumProxy.StratumListener.StratumClients.ContainsKey(client.Client.RemoteEndPoint))
            {
                ConsoleHelper.Log(client.Client.RemoteEndPoint, "Connected", LogLevel.Information);
                if (!RpcStratumProxy.StratumListener.StratumClients.TryGetValue(client.Client.RemoteEndPoint, out StratumClient stratumClient))
                {
                    // Remote endpoint not registered, add new client
                    ConsoleHelper.Log(client.Client.RemoteEndPoint, "Registered new client", LogLevel.Information);
                    stratumClient = new StratumClient(client);
                    RpcStratumProxy.StratumListener.StratumClients.TryAdd(client.Client.RemoteEndPoint, stratumClient);
                }

                await ProcessStratumClientAsync(stratumClient);
            }
        }

        private static async Task SendJobToClientAsync(StratumClient stratumClient)
        {
            if (stratumClient.MiningReady)
            {
                string[] workParams = await RpcStratumProxy.GetWorkAsync();
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

            bool disconnected;
            do
            {
                disconnected = stratumClient.TcpClient.IsDisconnected();
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

                            // Ignore login, return login success
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
                            string[] workParams = await RpcStratumProxy.GetWorkAsync();
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
                            bool workAccepted = await RpcStratumProxy.SubmitWorkAsync(submitWorkRequest.Params);

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
                        else
                        {
                            ConsoleHelper.Log(endpoint, $"Unhandled method {baseRequest.Method} from client", LogLevel.Warning);
                        }

                        ConsoleHelper.Log(endpoint, $"(O) {responseContent}", LogLevel.Debug);
                        await stratumClient.StreamWriter.WriteLineAsync($"{responseContent}");
                        await stratumClient.StreamWriter.FlushAsync();
                    }
                }
            } while (validRequestResponse && !disconnected);

            if (RpcStratumProxy.StratumListener.StratumClients.TryRemove(endpoint, out StratumClient stratumClientToFinalise))
            {
                stratumClientToFinalise.Dispose();
                ConsoleHelper.Log(endpoint, $"Client disconnected", LogLevel.Information);
            }
        }

        private static void Stop()
        {
            ConsoleHelper.Log("Shutdown", "Shutting down stratum server", LogLevel.Information);
            RpcStratumProxy.StratumListener.Stop();
            foreach (var stratumClient in RpcStratumProxy.StratumListener.StratumClients)
            {
                stratumClient.Value.Dispose();
                ConsoleHelper.Log("Shutdown", $"Disconnecting client {stratumClient.Key}", LogLevel.Information);
            }

            IsEnded = true;
        }
    }
}
