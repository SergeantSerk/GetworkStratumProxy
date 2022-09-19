using System.Text.Json.Serialization;

namespace GetworkStratumProxy.Rpc.Eth
{
    public abstract class JsonRpcResponse : JsonRpcMessage
    {
        [JsonPropertyName("error")]
        public Error Error { get; set; }
    }

    public class Error
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}
