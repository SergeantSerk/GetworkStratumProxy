using System.Text.Json.Serialization;

namespace GetworkStratumProxy.ConsoleApp.JsonRpc
{
    internal class BaseJsonRpc
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }
    }
}
