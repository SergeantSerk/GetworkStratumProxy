using Nethereum.Hex.HexTypes;

namespace GetworkStratumProxy.Rpc.Nicehash
{
    public sealed class MiningNotifyNotification : JsonRpcNotification
    {
        public MiningNotifyNotification(int jobId, string seedHash, string headerHash, bool clearJobQueue)
        {
            Method = "mining.notify";

            Params = new object[]
            {
                jobId.ToString(),
                new HexBigInteger(seedHash).HexValue.Replace("0x", ""),
                new HexBigInteger(headerHash).HexValue.Replace("0x", ""),
                clearJobQueue
            };
        }
    }
}
