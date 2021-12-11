using System.Text.Json.Serialization;

namespace GetworkStratumProxy.Rpc.EthProxy
{
    public class JsonRpcResponse : Rpc.JsonRpcResponse
    {
        [JsonPropertyName("result")]
        public object[] Result { get; set; }
    }
}
