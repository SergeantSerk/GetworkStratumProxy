using System.Text.Json.Serialization;

namespace GetworkStratumProxy.JsonRpc
{
    public class BaseRequest<T> : BaseJsonRpc
    {
        [JsonPropertyName("method")]
        public string Method { get; set; }

        [JsonPropertyName("worker")]
        public string Worker { get; set; }

        [JsonPropertyName("params")]
        public T Params { get; set; }
    }
}
