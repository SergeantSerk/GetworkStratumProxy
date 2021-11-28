using GetworkStratumProxy.Extension;
using System;
using System.Timers;

namespace GetworkStratumProxy.Node
{
    public sealed class PollingNode : BaseNode
    {
        private Timer Timer { get; set; }
        public int PollingInterval { get; private set; }

        public override event EventHandler<string[]> NewJobReceived;

        public PollingNode(Uri rpcUri, int pollingInterval) : base(rpcUri)
        {
            IsRunning = false;
            Timer = new Timer(pollingInterval)
            {
                AutoReset = true,
                Enabled = false
            };
            Timer.Elapsed += Timer_Elapsed;
        }

        private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!IsRunning)
            {
                Stop();
                Timer.Dispose();
                return;
            }

            LatestJob = await Web3.Eth.Mining.GetWork.SendRequestAsync();
            NewJobReceived?.Invoke(this, LatestJob);
        }

        public override void Start()
        {
            if (Timer.Enabled)
            {
                return;
            }

            ConsoleHelper.Log(GetType().Name, "Initialising getWork-polling timer and handler", LogLevel.Information);
            IsRunning = true;
            Timer.Enabled = true;
            Timer.Start();
        }

        public override void Stop()
        {
            if (!Timer.Enabled)
            {
                return;
            }

            ConsoleHelper.Log(GetType().Name, "Stopping getWork timer", LogLevel.Information);
            IsRunning = false;
            Timer.Stop();
        }
    }
}
