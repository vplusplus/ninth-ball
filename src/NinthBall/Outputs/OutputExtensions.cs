using NinthBall.Core;

namespace NinthBall
{
    /// <summary>
    /// Shared utilities used for output generation.
    /// </summary>
    internal static class OutputExtensions
    {
        internal static double AnnualizeChangePCT(this ReadOnlyMemory<SimYear> byYear)
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

        internal static double SumX(this ReadOnlyMemory<SimYear> byYear, Func<SimYear, double> fxValueSelector)
        {
            ArgumentNullException.ThrowIfNull(fxValueSelector);

            double sumAmount = 0.0;

            for (int i = 0; i < byYear.Length; i++)
            {
                sumAmount += fxValueSelector(byYear.Span[i]);
            }

            return sumAmount;
        }

        /// <summary>
        /// Cosmetics - Formats given number as Millions of $s (culture sensitive).
        /// </summary>
        internal static string Millions(this double value, int decimalPlaces = 1) => $"{(value / 1000000).ToString($"C{decimalPlaces}")} M";

    }
}
