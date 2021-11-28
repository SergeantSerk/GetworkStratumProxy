﻿using GetworkStratumProxy.Extension;
using GetworkStratumProxy.JsonRpc;
using GetworkStratumProxy.JsonRpc.Eth;
using GetworkStratumProxy.Node;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;

namespace GetworkStratumProxy.Proxy
{
    public sealed class EthProxy : BaseProxy
    {
        public override bool IsListening { get; protected set; }
        protected override TcpListener Server { get; set; }

        public EthProxy(BaseNode node, IPAddress address, int port) : base(node)
        {
            Server = new TcpListener(address, port);
            Node.NewJobReceived += Node_NewJobReceived;
        }

        private async void Node_NewJobReceived(object sender, string[] e)
        {
            if (!Clients.IsEmpty)
            {
                foreach (KeyValuePair<EndPoint, StratumClient> clientRecord in Clients)
                {
                    if (clientRecord.Value.TcpClient.IsDisconnected())
                    {
                        if (Clients.TryRemove(clientRecord))
                        {
                            clientRecord.Value.Dispose();
                            ConsoleHelper.Log(GetType().Name, $"Client {clientRecord.Key} disconnected", LogLevel.Information);
                        }
                    }
                    else
                    {
                        // Broadcast received work params to connected stratum clients
                        if (clientRecord.Value.MiningReady)
                        {
                            await SendJobToClientAsync(clientRecord, e);
                        }
                    }
                }
            }
        }

        private async Task SendJobToClientAsync(KeyValuePair<EndPoint, StratumClient> clientRecord, string[] workParams)
        {
            if (clientRecord.Value.MiningReady)
            {
                if (clientRecord.Value.PreviousWork == null || !clientRecord.Value.IsSameWork(workParams))
                {
                    clientRecord.Value.PreviousWork = workParams;
                    ConsoleHelper.Log(GetType().Name, $"Building fresh getWork from node for {clientRecord.Key}", LogLevel.Information);
                    var getworkResponse = new BaseResponse<string[]>
                    {
                        Id = 0,
                        JsonRpc = "2.0",
                        Error = null,
                        Result = workParams
                    };

                    string responseContentJson = JsonSerializer.Serialize(getworkResponse);
                    ConsoleHelper.Log(GetType().Name, $"Sending job to {clientRecord.Key}", LogLevel.Information);
                    ConsoleHelper.Log(GetType().Name, $"(O) {responseContentJson} -> {clientRecord.Key}", LogLevel.Debug);
                    await clientRecord.Value.StreamWriter.WriteLineAsync($"{responseContentJson}");
                    await clientRecord.Value.StreamWriter.FlushAsync();
                }
            }
            else
            {
                await Task.CompletedTask;
            }
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
                ConsoleHelper.Log(GetType().Name, "Could not accept connected, client was disposed", LogLevel.Warning);
                return;
            }

            if (!Clients.ContainsKey(client.Client.RemoteEndPoint))
            {
                ConsoleHelper.Log(GetType().Name, $"{client.Client.RemoteEndPoint} connected", LogLevel.Information);
                if (!Clients.TryGetValue(client.Client.RemoteEndPoint, out StratumClient stratumClient))
                {
                    // Remote endpoint not registered, add new client
                    ConsoleHelper.Log(GetType().Name, $"{client.Client.RemoteEndPoint} registered new client", LogLevel.Information);
                    stratumClient = new StratumClient(client);
                    Clients.TryAdd(client.Client.RemoteEndPoint, stratumClient);
                }

                await HandleClientRpcAsync(stratumClient);
            }
        }

        private async Task HandleClientRpcAsync(StratumClient stratumClient)
        {
            EndPoint endpoint = stratumClient.TcpClient.Client.RemoteEndPoint;

            while (!stratumClient.TcpClient.IsDisconnected())
            {
                string requestContent = await stratumClient.StreamReader.ReadLineAsync();
                if (!string.IsNullOrWhiteSpace(requestContent))
                {
                    ConsoleHelper.Log(GetType().Name, $"(I) {requestContent} <- {endpoint}", LogLevel.Debug);
                    BaseRequest<object> baseRequest = JsonSerializer.Deserialize<BaseRequest<object>>(requestContent);

                    string responseContent = null;
                    if (baseRequest.Method == "eth_submitLogin")
                    {
                        ConsoleHelper.Log(GetType().Name, $"Miner login from {endpoint}", LogLevel.Information);
                        var loginRequest = JsonSerializer.Deserialize<EthSubmitLoginRequest>(requestContent);

                        // Ignore login, return login success
                        ConsoleHelper.Log(GetType().Name, $"Miner login successful to {endpoint}", LogLevel.Information);
                        var loginResponse = new EthSubmitLoginResponse(loginRequest, true);
                        responseContent = JsonSerializer.Serialize(loginResponse);

                        ConsoleHelper.Log(GetType().Name, $"Sending login response to {endpoint}", LogLevel.Information);
                        stratumClient.MiningReady = true;
                    }
                    else if (baseRequest.Method == "eth_getWork")
                    {
                        ConsoleHelper.Log(GetType().Name, $"Miner getWork from {endpoint}", LogLevel.Information);
                        var getworkRequest = JsonSerializer.Deserialize<BaseRequest<object[]>>(requestContent);

                        ConsoleHelper.Log(GetType().Name, $"Polling initial getWork from node for {endpoint}", LogLevel.Information);
                        string[] workParams = Node.GetJob();
                        var getworkResponse = new BaseResponse<string[]>
                        {
                            Id = getworkRequest.Id,
                            JsonRpc = "2.0",
                            Error = null,
                            Result = workParams
                        };

                        responseContent = JsonSerializer.Serialize(getworkResponse);
                        ConsoleHelper.Log(GetType().Name, $"Sending job to {endpoint}", LogLevel.Information);
                    }
                    else if (baseRequest.Method == "eth_submitHashrate")
                    {
                        ConsoleHelper.Log(GetType().Name, $"Miner hashrate submit from {endpoint}", LogLevel.Information);
                        var submitHashrateRequest = JsonSerializer.Deserialize<BaseRequest<string[]>>(requestContent);
                        var submitHashrateResponse = new BaseResponse<bool>
                        {
                            Id = submitHashrateRequest.Id,
                            JsonRpc = "2.0",
                            Error = null,
                            Result = true
                        };

                        responseContent = JsonSerializer.Serialize(submitHashrateResponse);
                        ConsoleHelper.Log(GetType().Name, $"Acknowledging submitted hashrate by {endpoint}", LogLevel.Information);
                    }
                    else if (baseRequest.Method == "eth_submitWork")
                    {
                        ConsoleHelper.Log(GetType().Name, $"Miner {endpoint} submitted work", LogLevel.Information);
                        var submitWorkRequest = JsonSerializer.Deserialize<BaseRequest<string[]>>(requestContent);
                        string[] solution = submitWorkRequest.Params;
                        bool workAccepted = await Node.SendSolutionAsync(solution);

                        var submitWorkResponse = new BaseResponse<bool>
                        {
                            Id = submitWorkRequest.Id,
                            JsonRpc = "2.0",
                            Error = null,
                            Result = workAccepted
                        };
                        ConsoleHelper.Log(GetType().Name, $"Solution found by {endpoint} " +
                            $"was {(workAccepted ? "accepted" : "rejected")}", workAccepted ? LogLevel.Success : LogLevel.Error);

                        responseContent = JsonSerializer.Serialize(submitWorkResponse);
                        ConsoleHelper.Log(GetType().Name, $"Acknowledging submitted work by {endpoint}", LogLevel.Information);
                    }
                    else
                    {
                        ConsoleHelper.Log(GetType().Name, $"Unhandled method {baseRequest.Method} from {endpoint}", LogLevel.Warning);
                    }

                    ConsoleHelper.Log(GetType().Name, $"(O) {responseContent} -> {endpoint}", LogLevel.Debug);
                    await stratumClient.StreamWriter.WriteLineAsync($"{responseContent}");
                    await stratumClient.StreamWriter.FlushAsync();
                }
            }

            if (Clients.TryRemove(endpoint, out StratumClient stratumClientToFinalise))
            {
                stratumClientToFinalise.Dispose();
                ConsoleHelper.Log(GetType().Name, $"{endpoint} disconnected", LogLevel.Information);
            }
        }
    }
}