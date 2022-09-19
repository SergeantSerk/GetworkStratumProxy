using Nethereum.Hex.HexTypes;

namespace GetworkStratumProxy.Rpc.Eth
{
    public class EthWork
    {
        public HexBigInteger Header { get; set; }
        public HexBigInteger Seed { get; set; }
        public HexBigInteger Target { get; set; }

        public EthWork(string[] ethWork)
        {
            Header = new HexBigInteger(ethWork[0]);
            Seed = new HexBigInteger(ethWork[1]);
            Target = new HexBigInteger(ethWork[2]);
        }

        public bool Equals(EthWork ethWork)
        {
            return ethWork.Header == Header
                && ethWork.Seed == Seed
                && ethWork.Target == Target;
        }

        public string[] ToArray()
        {
            return new string[] { Header.HexValue, Seed.HexValue, Target.HexValue };
        }
    }
}
