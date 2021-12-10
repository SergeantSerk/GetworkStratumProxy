namespace GetworkStratumProxy.Rpc.EthProxy
{
    public sealed class NotifyJobResponse : JsonResponse
    {
        public NotifyJobResponse(string[] job)
        {
            Id = 0;
            JsonRpc = "2.0";
            Result = job;
        }
    }
}
