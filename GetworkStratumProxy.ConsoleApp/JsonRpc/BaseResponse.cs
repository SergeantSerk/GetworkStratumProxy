using System.Text.Json.Serialization;

namespace GetworkStratumProxy.ConsoleApp.JsonRpc
{
    internal class BaseResponse<T> : BaseJsonRpc
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; }

        [JsonPropertyName("result")]
        public T Result { get; set; }

        [JsonPropertyName("error")]
        public Error Error { get; set; }

        public BaseResponse()
        {
            JsonRpc = "2.0";
        }
    }

    internal class Error
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}
