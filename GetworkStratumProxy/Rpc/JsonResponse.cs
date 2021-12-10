using System.Text.Json.Serialization;

namespace GetworkStratumProxy.Rpc
{
    public abstract class JsonResponse : JsonMessage
    {
        [JsonPropertyName("result")]
        public object[] Result { get; set; }

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
