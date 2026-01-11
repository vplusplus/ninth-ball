
namespace NinthBall.Outputs
{
    public readonly record struct Percentile(double Pctl, string FriendlyName)
    {
        public const double Target = 0.20;
        public string PctlName => $"{Pctl * 100:0}th";
    }

    public readonly record struct SimOutput
    (
        IReadOnlyList<Percentile> Percentiles,
        IReadOnlyList<CID> HtmlColumns,
        IReadOnlyList<CID> ExcelColumns
    );
}
