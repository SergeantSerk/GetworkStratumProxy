namespace GetworkStratumProxy.Rpc.Nicehash
{
    public sealed class SetDifficultyNotification : JsonRpcNotification
    {
        public SetDifficultyNotification(decimal difficulty)
        {
            Method = "mining.set_difficulty";
            Params = new object[] { difficulty };
        }
    }
}
