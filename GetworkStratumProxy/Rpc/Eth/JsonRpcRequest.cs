using System.Text.Json.Serialization;

namespace GetworkStratumProxy.Rpc.Eth
{
    internal class JsonRpcRequest : JsonRpcMessage
    {
        [JsonPropertyName("method")]
        public string Method { get; set; }
        [JsonPropertyName("params")]
        public object[] Params { get; set; }
    }
}