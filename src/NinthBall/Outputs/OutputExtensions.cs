using NinthBall.Core;

namespace NinthBall
{
    /// <summary>
    /// Shared utilities used for output generation.
    /// </summary>
    internal static class OutputExtensions
    {
        public static double AnnualizeChangePCT(this ReadOnlyMemory<SimYear> byYear)
        {
            double compoundReturn = 1;
            int count = 0;

            for (int i = 0; i < byYear.Length; i++)
            {
                var r = byYear.Span[i].ChangePCT;
                checked { compoundReturn *= (1 + r); }
                count++;
            }

            return 0 == count ? 0.0 : Math.Pow(compoundReturn, 1.0 / count) - 1;
        }
    }
}
