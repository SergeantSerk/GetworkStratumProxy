using GetworkStratumProxy.Extension;
using GetworkStratumProxy.Rpc;
using System;
using System.Timers;

namespace GetworkStratumProxy.Node
{
    /// <summary>
    /// Interval-based work polling system with work polled from node.
    /// </summary>
    public sealed class PollingNode : BaseNode
    {
        private Timer Timer { get; set; }
        public int PollingInterval { get; private set; }

        internal override event EventHandler<EthWork> NewWorkReceived;

        /// <summary>
        /// Initialises a work polling subsystem, initially disabled and with specified polling interval, polling for work from node RPC.
        /// </summary>
        /// <param name="rpcUri">Node RPC url.</param>
        /// <param name="pollingInterval">Intervals, in milliseconds, to poll work in.</param>
        public PollingNode(Uri rpcUri, int pollingInterval) : base(rpcUri)
        {
            Timer = new Timer(pollingInterval)
            {
                AutoReset = true,
                Enabled = false
            };
            Timer.Elapsed += Timer_Elapsed;
        }

        private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (DisposedValue)
            {
                Stop();
                Timer.Dispose();
                return;
            }

            string[] receivedEthWorkRaw = await Web3.Eth.Mining.GetWork.SendRequestAsync();
            var receivedEthWork = new EthWork(receivedEthWorkRaw);
            if (TryUpdateWork(receivedEthWork))
            {
                ConsoleHelper.Log(GetType().Name, $"Received latest work " +
                    $"({receivedEthWork.Header.HexValue[..Constants.WorkHeaderCharactersPrefixCount]}...) from polled node", LogLevel.Debug);
                NewWorkReceived?.Invoke(this, LatestEthWork);
            }
        }

        public override void Start()
        {
            if (Timer.Enabled)
            {
                return;
            }

            ConsoleHelper.Log(GetType().Name, "Initialising getWork-polling timer and handler", LogLevel.Information);
            Timer.Start();
        }

        public override void Stop()
        {
            if (!Timer.Enabled)
            {
                return;
            }

            ConsoleHelper.Log(GetType().Name, "Stopping getWork timer", LogLevel.Information);
            Timer.Stop();
        }
    }
}
