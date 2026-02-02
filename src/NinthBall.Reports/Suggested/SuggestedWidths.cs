
using static NinthBall.Reports.WidthHint;

namespace NinthBall.Reports
{
    internal enum WidthHint { W1x, W2x, W3x, W4x }

    internal static partial class Suggested
    {
        /// <summary>
        /// Provides suggested cell width hint for given column.
        /// Returns default width hint if none defined.
        /// </summary>
        internal static WidthHint GetWidthHint(this CID cid) => Widths.TryGetValue(cid, out var widthHint) ? widthHint : W2x;

        static readonly IReadOnlyDictionary<CID, WidthHint> Widths = new Dictionary<CID, WidthHint>()

            // Narrow columns
            .WithColumnWidths( WidthHint.W1x,

                CID.Year,
                CID.Age,
                CID.LikeYear

                //CID.JanPreTaxAlloc,
                //CID.JanPostTaxAlloc,
                //CID.DecPreTaxAlloc,
                //CID.DecPostTaxAlloc,


                //CID.Infl,
                //CID.ROIStocks,
                //CID.ROIBonds,
                //CID.ROI,
                //CID.AnnROI,
                //CID.RealCAGR,
                //CID.TaxPCT,
                //CID.FedTaxPCT,
                //CID.StaTaxPCT,
                //CID.MTROrdInc,
                //CID.MTRCapGain,
                //CID.MTRState
            )
            .AsReadOnly();

        private static Dictionary<CID, WidthHint> WithColumnWidths(this Dictionary<CID, WidthHint> dict, WidthHint hint, params CID[] columnIds)
        {
            foreach (var columnId in columnIds) dict[columnId] = hint;
            return dict;
        }

    }
}
