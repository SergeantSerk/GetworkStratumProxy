using System.Text.Json.Serialization;

namespace GetworkStratumProxy.ConsoleApp.JsonRpc
{
    internal class BaseRequest<T> : BaseJsonRpc
    {
        [JsonPropertyName("method")]
        public string Method { get; set; }

        [JsonPropertyName("worker")]
        public string Worker { get; set; }

        [JsonPropertyName("params")]
        public T Params { get; set; }
    }
}
