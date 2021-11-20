namespace GetworkStratumProxy.JsonRpc.Eth
{
    public class EthSubmitLoginResponse : BaseResponse<bool>
    {
        public EthSubmitLoginResponse(EthSubmitLoginRequest loginRequest, bool success)
        {
            Id = loginRequest.Id;
            Result = success;
        }
    }
}
