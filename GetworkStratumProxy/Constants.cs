using Nethereum.Hex.HexTypes;
using System.Numerics;

namespace GetworkStratumProxy
{
    internal class Constants
    {
        public const int JobCharactersPrefixCount = 10;

        private static readonly BigInteger MaxTarget = BigInteger.Pow(16, 64) - 1;
        private static readonly BigInteger DifficultyDenom = BigInteger.Pow(2, 32);

        public static decimal GetDifficultyFromTarget(HexBigInteger currentTarget)
        {
            var calculatedDifficulty = MaxTarget / currentTarget.Value;
            return (decimal)calculatedDifficulty / (decimal)DifficultyDenom;
        }

        public static HexBigInteger GetTargetFromDifficulty(decimal difficulty)
        {
            var calculatedDifficulty = difficulty * (decimal)DifficultyDenom;
            return new HexBigInteger(MaxTarget / (BigInteger)calculatedDifficulty);
        }
    }
}
