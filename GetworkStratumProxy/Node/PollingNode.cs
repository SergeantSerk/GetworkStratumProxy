using GetworkStratumProxy.Extension;
using System;
using System.Timers;

namespace GetworkStratumProxy.Node
{
    /// <summary>
    /// Interval-based job polling system with work polled from node.
    /// </summary>
    public sealed class PollingNode : BaseNode
    {
        private Timer Timer { get; set; }
        public int PollingInterval { get; private set; }

        internal override event EventHandler<string[]> NewJobReceived;

        /// <summary>
        /// Initialises a job polling subsystem, initially disabled and with specified polling interval, polling for jobs from node RPC.
        /// </summary>
        /// <param name="rpcUri">Node RPC url.</param>
        /// <param name="pollingInterval">Intervals, in milliseconds, to poll jobs in.</param>
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

            var receivedJob = await Web3.Eth.Mining.GetWork.SendRequestAsync();
            if (TryUpdateWork(receivedJob))
            {
                ConsoleHelper.Log(GetType().Name, $"Received latest job " +
                    $"({LatestJob[0][..Constants.JobCharactersPrefixCount]}...) from polled node", LogLevel.Debug);
                NewJobReceived?.Invoke(this, LatestJob);
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
