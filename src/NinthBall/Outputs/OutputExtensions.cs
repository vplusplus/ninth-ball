using NinthBall.Core;

namespace NinthBall
{
    /// <summary>
    /// Shared utilities used for output generation.
    /// </summary>
    internal static class OutputExtensions
    {




        /// <summary>
        /// Cosmetics - Formats given number as Millions of $s (culture sensitive).
        /// </summary>
        internal static string Millions(this double value, int decimalPlaces = 1) => $"{(value / 1000000).ToString($"C{decimalPlaces}")} M";

    }
}
