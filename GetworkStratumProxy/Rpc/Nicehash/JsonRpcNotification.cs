using System.Text.Json.Serialization;

namespace GetworkStratumProxy.Rpc.Nicehash
{
    public abstract class JsonRpcNotification : JsonRpcResponse
    {
        [JsonPropertyName("method")]
        public string Method { get; set; }

        [JsonPropertyName("params")]
        public object[] Params { get; set; }

        public JsonRpcNotification()
        {
            Id = null;
        }
    }
}
