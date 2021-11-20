using GetworkStratumProxy.Stratum;
using Nethereum.Web3;
using System;
using System.Net;
using System.Threading.Tasks;

namespace GetworkStratumProxy
{
    public class RpcStratumProxy
    {
        public IWeb3 Web3 { get; private set; }
        public StratumListener StratumListener { get; private set; }

        public RpcStratumProxy(Uri rpcUri, IPAddress stratumAddress, int stratumPort)
        {
            Web3 = new Web3(rpcUri.AbsoluteUri);
            StratumListener = new StratumListener(stratumAddress, stratumPort);
        }

        public async Task<string[]> GetWorkAsync()
        {
            return await Web3.Eth.Mining.GetWork.SendRequestAsync();
        }

        public async Task<bool> SubmitWorkAsync(string[] work)
        {
            return await SubmitWorkAsync(nonce: work[0], header: work[1], mix: work[2]);
        }

        public async Task<bool> SubmitWorkAsync(string nonce, string header, string mix)
        {
            return await Web3.Eth.Mining.SubmitWork.SendRequestAsync(nonce: nonce, header: header, mix: mix);
        }
    }
}
