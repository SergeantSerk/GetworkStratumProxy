using Nethereum.Web3;
using System;
using System.Threading.Tasks;

namespace GetworkStratumProxy.Node
{
    public abstract class BaseNode : INode, IDisposable
    {
        private bool disposedValue;

        public bool IsRunning { get; protected set; }
        public IWeb3 Web3 { get; private set; }
        public string[] LatestJob { get; protected set; }

        public abstract event EventHandler<string[]> NewJobReceived;

        public BaseNode(Uri rpcUri)
        {
            Web3 = new Web3(rpcUri.AbsoluteUri);
        }

        public abstract void Start();

        public abstract void Stop();

        public string[] GetJob()
        {
            return LatestJob;
        }

        public async Task<bool> SendSolutionAsync(string[] solution)
        {
            return await Web3.Eth.Mining.SubmitWork.SendRequestAsync(solution[0], solution[1], solution[2]);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    IsRunning = false;
                    Stop();
                }

                Web3 = null;
                LatestJob = null;
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
