using System.Text.Json.Serialization;

namespace GetworkStratumProxy.Rpc.Eth
{
    public abstract class JsonRpcMessage
    {
        [JsonPropertyName("id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public int? Id { get; set; }

        [JsonPropertyName("jsonrpc")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string JsonRpc { get; set; }
    }
}
