using System.Text.Json.Serialization;

namespace GetworkStratumProxy.JsonRpc
{
    public class BaseJsonRpc
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }
    }
}
