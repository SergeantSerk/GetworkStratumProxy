namespace GetworkStratumProxy.Rpc.EthProxy
{
    public sealed class NewEthWorkNotification : JsonRpcNotification
    {
        public NewEthWorkNotification(EthWork ethWork)
        {
            Id = 0;
            JsonRpc = "2.0";
            Result = ethWork.ToArray();
        }
    }
}
