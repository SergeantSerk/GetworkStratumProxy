using System.Text.Json.Serialization;

namespace GetworkStratumProxy.Rpc
{
    public abstract class JsonMessage
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; }
    }
}
