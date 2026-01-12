
namespace NinthBall.Outputs
{
    public readonly record struct Percentile(double Pctl, string FriendlyName)
    {
        public const double Target = 0.20;

        public string PctlName
        {
            get
            {
                int val = (int)Math.Round(Pctl * 100);
                string suffix = val % 100 is >= 11 and <= 13 ? "th" : (val % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
                return $"{val}{suffix}";
            }
        }
    }

    /// <summary>
    /// Immutable structure that describes output configurations.
    /// </summary>
    public readonly record struct SimOutput
    (
        IReadOnlyList<Percentile> Percentiles,
        IReadOnlyList<CID> HtmlColumns,
        IReadOnlyList<CID> ExcelColumns
    );
}
