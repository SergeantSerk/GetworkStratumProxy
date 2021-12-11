namespace GetworkStratumProxy.Rpc.EthProxy
{
    public sealed class NewJobNotification : JsonRpcNotification
    {
        public NewJobNotification(string[] job)
        {
            Id = 0;
            JsonRpc = "2.0";
            Result = job;
        }
    }
}
