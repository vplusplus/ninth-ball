
namespace NinthBall.Outputs
{
    internal static partial class Suggested
    {
        internal static string GetColTitle(this CID cid) => ColumnTitles.TryGetValue(cid, out var colTitle) && null != colTitle ? colTitle : string.Empty;

        static readonly IReadOnlyDictionary<CID, string> ColumnTitles = new Dictionary<CID, string>()
        {
            
            [CID.JanValue]      = "Approx value (401K x 75% + Inv x 85%)",
            [CID.DecValue]      = "Approx value (401K x 75% + Inv x 85%)",

            [CID.ROI]           = "Effective ROI (StockAlloc x StockROI + BondAlloc x BondROI). Bottom line: Annualized effective ROI at the last good year.",
            [CID.AnnROI]        = "Running annualized effective ROI from year #0. Bottom line: Annualized effective ROI at the last good year.",
            [CID.RealCAGR]      = "Annualized Real Effective ROI (Purchasing Power). Benchmarked against the '4% Rule' survival threshold (1.9%).",



        }.AsReadOnly();

    }
}
