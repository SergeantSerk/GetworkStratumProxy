using System.Text.Json.Serialization;

namespace GetworkStratumProxy.Rpc.EthProxy
{
    public class NotifyJobResponse
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; }

        [JsonPropertyName("result")]
        public string[] Result { get; set; }

        [JsonPropertyName("error")]
        public Error Error { get; set; }

        public NotifyJobResponse(string[] job, long id = 0)
        {
            Id = id;
            JsonRpc = "2.0";
            Result = job;
        }
    }

    public class Error
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}
