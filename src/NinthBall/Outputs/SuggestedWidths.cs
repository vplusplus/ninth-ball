
using static NinthBall.Outputs.WidthHint;

namespace NinthBall.Outputs
{
    internal enum WidthHint { W1x, W2x, W3x, W4x }

    internal static partial class ColumnDefinitions
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
            [CID.ROIAnn] = W1x,
            [CID.ROIStocks] = W1x,
            [CID.ROIBonds] = W1x,
            [CID.ROICash] = W1x,

        }.AsReadOnly();

    }
}
