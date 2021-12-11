using System.Text.Json.Serialization;

namespace GetworkStratumProxy.Rpc
{
    public abstract class JsonRpcMessage
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; }
    }
}
