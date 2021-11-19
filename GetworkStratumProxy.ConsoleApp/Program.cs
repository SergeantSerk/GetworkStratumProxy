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
    public class StreamHandler : IDisposable
    {
        public TcpClient TcpClient { get; set; }
        public StreamReader StreamReader { get; set; }
        public StreamWriter StreamWriter { get; set; }

        public bool MiningReady { get; set; }
        public string[] PreviousWork { get; set; } = null;

        public StreamHandler(TcpClient tcpClient, StreamReader streamReader, StreamWriter streamWriter)
        {
            TcpClient = tcpClient;
            StreamReader = streamReader;
            StreamWriter = streamWriter;
        }

        public void Dispose()
        {
            StreamWriter.Dispose();
            StreamReader.Dispose();
            TcpClient.Dispose();
        }
    }

    public static class Program
    {
        public static bool IsRunning { get; set; } = true;
        public static bool IsEnded { get; private set; } = false;
        public static IWeb3Geth Geth { get; private set; }
        private static TcpListener StratumListener { get; } = new TcpListener(IPAddress.Any, 3131);
        public static ConcurrentDictionary<EndPoint, StreamHandler> StratumClients { get; } = new ConcurrentDictionary<EndPoint, StreamHandler>();

        public static async Task Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Specify URI for RPC node e.g. http://127.0.0.1:8545/");
                return;
            }

            var rpcUri = new Uri(args[^1]);
            Geth = new Web3Geth(rpcUri.AbsoluteUri);
            int sleepPeriod = 500;

            Console.WriteLine("Initialising getwork timer and handler...");
            new Timer(sleepPeriod) { AutoReset = true, Enabled = true }.Elapsed += async (o, e) =>
            {
                if (!IsRunning)
                {
                    Console.WriteLine("Disposing getwork loop...");
                    (o as Timer).Dispose();
                }
                else if (!StratumClients.IsEmpty)
                {
                    foreach (var stratumClient in StratumClients)
                    {
                        var state = stratumClient.Value.TcpClient.GetState();
                        if (state == TcpState.Closing ||
                            state == TcpState.CloseWait ||
                            state == TcpState.Closed)
                        {
                            if (StratumClients.TryRemove(stratumClient))
                            {
                                stratumClient.Value.Dispose();
                                Log(stratumClient.Key, $"Client disconnected");
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

            Console.WriteLine("Starting stratum server...");
            StratumListener.Start();

            Console.CancelKeyPress += (o, e) =>
            {
                e.Cancel = true;
                Console.WriteLine($"Caught {e.SpecialKey}, stopping...");
                IsRunning = false;
                Stop();
            };

            Console.WriteLine("Ready and waiting for new stratum clients.");
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

            Console.WriteLine("Exited gracefully.");
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
            }

            if (client != null && !StratumClients.ContainsKey(client.Client.RemoteEndPoint))
            {
                Log(client.Client.RemoteEndPoint, "Connected");
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
                    Log(streamHandler.TcpClient.Client.RemoteEndPoint, "Building fresh getwork from node...");
                    var getworkResponse = new BaseResponse<string[]>
                    {
                        Id = 0,
                        JsonRpc = "2.0",
                        Error = null,
                        Result = workParams
                    };

                    string responseContentJson = JsonSerializer.Serialize(getworkResponse);
                    Log(streamHandler.TcpClient.Client.RemoteEndPoint, "Sending job");
                    Log(streamHandler.TcpClient.Client.RemoteEndPoint, $"(O) {responseContentJson}");
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
                        Log(endpoint, $"(I) {requestContent}");
                        BaseRequest<object> baseRequest = JsonSerializer.Deserialize<BaseRequest<object>>(requestContent);

                        string responseContent = null;
                        if (baseRequest.Method == "eth_submitLogin")
                        {
                            Log(endpoint, "Miner login");
                            var loginRequest = JsonSerializer.Deserialize<EthSubmitLoginRequest>(requestContent);

                            Log(endpoint, "Miner login successful");
                            var loginResponse = new EthSubmitLoginResponse(loginRequest, true);
                            responseContent = JsonSerializer.Serialize(loginResponse);

                            Log(endpoint, "Sending login response");
                            streamHandler.MiningReady = true;
                        }
                        else if (baseRequest.Method == "eth_getWork")
                        {
                            Log(endpoint, "Miner getwork");
                            var getworkRequest = JsonSerializer.Deserialize<BaseRequest<object[]>>(requestContent);

                            Console.WriteLine("Polling getwork from node...");
                            string[] workParams = await Geth.Eth.Mining.GetWork.SendRequestAsync();
                            var getworkResponse = new BaseResponse<string[]>
                            {
                                Id = getworkRequest.Id,
                                JsonRpc = "2.0",
                                Error = null,
                                Result = workParams
                            };

                            responseContent = JsonSerializer.Serialize(getworkResponse);
                            Log(endpoint, "Sending job");
                        }
                        else if (baseRequest.Method == "eth_submitHashrate")
                        {
                            Log(endpoint, "Miner hashrate submit");
                            var submitHashrateRequest = JsonSerializer.Deserialize<BaseRequest<string[]>>(requestContent);
                            var submitHashrateResponse = new BaseResponse<bool>
                            {
                                Id = submitHashrateRequest.Id,
                                JsonRpc = "2.0",
                                Error = null,
                                Result = true
                            };

                            responseContent = JsonSerializer.Serialize(submitHashrateResponse);
                            Log(endpoint, "Acknowledging submitted hashrate");
                        }
                        else if (baseRequest.Method == "eth_submitWork")
                        {
                            Log(endpoint, "Miner submit work");
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
                            Log(endpoint, "acknowledging submitted hashrate");
                        }

                        Log(endpoint, $"(O) {responseContent}");
                        await streamHandler.StreamWriter.WriteLineAsync($"{responseContent}");
                        await streamHandler.StreamWriter.FlushAsync();
                    }
                }
            } while (validRequestResponse && !disconnected);

            if (StratumClients.TryRemove(endpoint, out StreamHandler streamHandlerToFinalise))
            {
                streamHandlerToFinalise.Dispose();
                Log(endpoint, $"Client disconnected");
            }
        }

        private static void Log(EndPoint endpoint, string message)
        {
            Console.WriteLine($"[{endpoint,-15}] {message}");
        }

        private static void Stop()
        {
            Console.WriteLine("Shutting down stratum server...");
            StratumListener.Stop();
            foreach (var stratumClient in StratumClients)
            {
                stratumClient.Value.Dispose();
                Console.WriteLine($"Disconnecting client {stratumClient.Key}...");
            }

            IsEnded = true;
        }
    }
}
