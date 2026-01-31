
namespace NinthBall.Reports
{
    internal static class OutputExtensions
    {
        extension (double value)
        {
            // Cosmetics - Formats given number as percentile with 1st, nnd, 3rd, Nth suffix.
            public string PctlName 
            {
                get
                {
                    int val = (int)Math.Round(value * 100);
                    string suffix = val % 100 is >= 11 and <= 13 ? "th" : (val % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
                    return $"{val}{suffix}";
                }
            }

            // Cosmetics - Formats given number as Millions of $s (culture sensitive).
            public string Millions(int decimalPlaces = 1) => $"{(value / 1000000).ToString($"C{decimalPlaces}")} M";
        }
    }
}
