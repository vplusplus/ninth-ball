
using static NinthBall.Outputs.FormatHint;

namespace NinthBall.Outputs
{
    internal enum FormatHint { F0, C0, C1, C2, P0, P1, P2 }

    internal static partial class ColumnDefinitions
    {
        /// <summary>
        /// Provides suggested cell format hint for given column.
        /// Returns default formatting choice if none defined.
        /// </summary>
        internal static FormatHint GetFormatHint(this CID cid) => Formats.TryGetValue(cid, out var hint) ? hint : FormatHint.C0;


        static readonly IReadOnlyDictionary<CID, FormatHint> Formats = new Dictionary<CID, FormatHint>()
        {
            [CID.Year]  = F0,
            [CID.Age]   = F0,

            [CID.JanPreTaxAlloc]    = P0,
            [CID.JanPostTaxAlloc]   = P0,
            [CID.DecPreTaxAlloc]    = P0,
            [CID.DecPostTaxAlloc]   = P0,

            [CID.LikeYear]      = F0,
            [CID.ROIStocks]     = P1,
            [CID.ROIBonds]      = P1,
            [CID.ROICash]       = P1,
            [CID.InflationRate] = P1,
            [CID.ROI]           = P1,
            [CID.AnnROI]        = P1,

        }.AsReadOnly();

    }
}
