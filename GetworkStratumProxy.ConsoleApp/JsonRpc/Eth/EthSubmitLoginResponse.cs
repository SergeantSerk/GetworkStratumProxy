namespace GetworkStratumProxy.ConsoleApp.JsonRpc.Eth
{
    internal class EthSubmitLoginResponse : BaseResponse<bool>
    {
        public EthSubmitLoginResponse(EthSubmitLoginRequest loginRequest, bool success)
        {
            Id = loginRequest.Id;
            Result = success;
        }
    }
}
