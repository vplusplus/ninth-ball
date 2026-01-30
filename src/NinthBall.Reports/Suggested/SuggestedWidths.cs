
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
        {
            [CID.Year] = W1x,
            [CID.Age] = W1x,

            [CID.LikeYear] = W1x,
            [CID.ROI] = W1x,
            [CID.AnnROI] = W1x,
            [CID.ROIStocks] = W1x,
            [CID.ROIBonds] = W1x,
            [CID.InflationRate] = W1x,
            [CID.RealCAGR]      = W1x,

            [CID.JanPreTaxAlloc] = W1x,
            [CID.JanPostTaxAlloc] = W1x,
            [CID.DecPreTaxAlloc] = W1x,
            [CID.DecPostTaxAlloc] = W1x,


        }.AsReadOnly();

    }
}
