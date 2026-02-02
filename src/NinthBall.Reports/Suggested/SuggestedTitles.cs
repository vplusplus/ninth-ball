
using NinthBall.Core;

namespace NinthBall.Reports
{
    internal static partial class Suggested
    {
        internal static string GetColTitle(this CID cid) => ColumnTitles.TryGetValue(cid, out var colTitle) && null != colTitle ? colTitle : string.Empty;

        static readonly IReadOnlyDictionary<CID, string> ColumnTitles = new Dictionary<CID, string>()
        {
            
            [CID.JanNet]        = "Jan - Approx net worth less taxes liability (401K x 75% + Inv x 85%)",
            [CID.DecNet]        = "Jan - Approx net worth less taxes liability (401K x 75% + Inv x 85%)",

            [CID.TaxPCT]        = "For every $ that came in, what PCT went to taxes",
            [CID.FedTaxPCT]     = "For every $ that came in, what PCT went to Federal taxes",
            [CID.StaTaxPCT]     = "For every $ that came in, what PCT went to State taxes",

            [CID.ROI]           = "Effective ROI (StockAlloc x StockROI + BondAlloc x BondROI). Bottom line: Annualized effective ROI at the last good year.",
            [CID.AnnROI]        = "Running annualized effective ROI from year #0. Bottom line: Annualized effective ROI at the last good year.",
            [CID.RealCAGR]      = "Annualized Real Effective ROI (Purchasing Power). Benchmarked against the '4% Rule' survival threshold (1.9%).",


        }.AsReadOnly();

    }
}
