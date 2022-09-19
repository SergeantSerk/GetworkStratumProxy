using Nethereum.Hex.HexTypes;
using System.Numerics;

namespace GetworkStratumProxy
{
    internal class Constants
    {
        public const int WorkHeaderCharactersPrefixCount = 10;

        private static readonly BigInteger MaxTarget = BigInteger.Pow(16, 64) - 1;
        private const long BaseDifficultyOfOne = 4294967296;

        public static decimal GetDifficultyFromTarget(HexBigInteger currentTarget)
        {
            var calculatedDifficulty = MaxTarget / currentTarget.Value;
            return (decimal)calculatedDifficulty / BaseDifficultyOfOne;
        }

        public static HexBigInteger GetTargetFromDifficulty(decimal difficulty)
        {
            var calculatedDifficulty = difficulty * BaseDifficultyOfOne;
            return new HexBigInteger(MaxTarget / (BigInteger)calculatedDifficulty);
        }

        public static decimal GetDifficultySize(HexBigInteger currentTarget)
        {
            var targetDiff = GetDifficultyFromTarget(currentTarget);
            return BaseDifficultyOfOne * targetDiff;
        }

        public static decimal GetDifficultySize(decimal difficulty)
        {
            return BaseDifficultyOfOne * difficulty;
        }
    }
}
