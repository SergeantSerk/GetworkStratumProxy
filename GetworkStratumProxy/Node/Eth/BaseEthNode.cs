using GetworkStratumProxy.Rpc.Eth;
using Nethereum.Web3;
using System;

namespace GetworkStratumProxy.Node.Eth
{
    public abstract class BaseEthNode : INode, IDisposable
    {
        internal abstract event EventHandler<EthWork> NewWorkReceived;
        public abstract void Start();
        public abstract void Stop();

        protected bool DisposedValue { get; private set; }
        internal IWeb3 Web3 { get; private set; }

        /// <summary>
        /// Track latest work for tracking incoming new works
        /// </summary>
        protected EthWork LatestEthWork { get; set; }

        public BaseEthNode(Uri rpcUri)
        {
            Web3 = new Web3(rpcUri.AbsoluteUri);
        }

        /// <summary>
        /// Update tracked work with newly received work and return update result.
        /// </summary>
        /// <param name="ethWork">Work to compare and update against current tracked work.</param>
        /// <returns>Returns <see langword="true"/> if work is different from previous tracked work, else <see langword="false"/>.</returns>
        protected bool TryUpdateWork(EthWork ethWork)
        {
            if (LatestEthWork == null || !LatestEthWork.Equals(ethWork))
            {
                LatestEthWork = ethWork;
                return true;
            }
            else
            {
                return false;
            }
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
                LatestEthWork = null;
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
