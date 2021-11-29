using Nethereum.Web3;
using System;
using System.Threading.Tasks;

namespace GetworkStratumProxy.Node
{
    public abstract class BaseNode : INode, IDisposable
    {
        protected bool DisposedValue { get; private set; }

        public IWeb3 Web3 { get; private set; }
        public string[] LatestJob { get; protected set; }

        public abstract event EventHandler<string[]> NewJobReceived;

        public BaseNode(Uri rpcUri)
        {
            Web3 = new Web3(rpcUri.AbsoluteUri);
        }

        public abstract void Start();

        public abstract void Stop();

        /// <summary>
        /// Update tracked job with newly received job and return update result.
        /// </summary>
        /// <param name="newJob">New job received from node.</param>
        /// <returns>Returns <see langword="true"/> if job is different from previous tracked job, else <see langword="false"/>.</returns>
        protected bool TryUpdateWork(string[] newJob)
        {
            if (LatestJob == null || LatestJob.Length != newJob.Length)
            {
                LatestJob = newJob;
                return true;
            }

            for (int i = 0; i < LatestJob.Length; ++i)
            {
                if (LatestJob[i] != newJob[i])
                {
                    LatestJob = newJob;
                    return true;
                }
            }

            return false;
        }

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
            if (!DisposedValue)
            {
                if (disposing)
                {
                    Stop();
                }

                Web3 = null;
                LatestJob = null;
                DisposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
