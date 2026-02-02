
using DocumentFormat.OpenXml.Office.Y2022.FeaturePropertyBag;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static NinthBall.Reports.FormatHint;

namespace NinthBall.Reports
{
    internal enum FormatHint { F0, F1, F2, C0, C1, C2, P0, P1, P2 }

    internal static partial class Suggested
    {
        /// <summary>
        /// Provides suggested cell format hint for given column.
        /// Returns default formatting choice if none defined.
        /// </summary>
        internal static FormatHint GetFormatHint(this CID cid) => Formats.TryGetValue(cid, out var hint) ? hint : FormatHint.C0;


        static readonly IReadOnlyDictionary<CID, FormatHint> Formats = new Dictionary<CID, FormatHint>()
            .WithColumnFormats(FormatHint.F0,
                CID.Year,
                CID.Age,
                CID.LikeYear
            )
            .WithColumnFormats( FormatHint.F2, 
                CID.MXInf, 
                CID.MXInfFedTax, 
                CID.MXInfStaTax, 
                CID.MXGrowth
            )
            .WithColumnFormats( FormatHint.P0,
                CID.JanPreTaxAlloc,
                CID.JanPostTaxAlloc,
                CID.DecPreTaxAlloc,
                CID.DecPostTaxAlloc
            )
            .WithColumnFormats( FormatHint.P1,
                CID.Infl,
                CID.ROIStocks,
                CID.ROIBonds,
                CID.ROI,
                CID.AnnROI,
                CID.RealCAGR,
                CID.TaxPCT,
                CID.FedTaxPCT,
                CID.StaTaxPCT,
                CID.MTROrdInc,
                CID.MTRCapGain,
                CID.MTRState
            )
            .AsReadOnly();

        private static Dictionary<CID, FormatHint> WithColumnFormats(this Dictionary<CID, FormatHint> dict, FormatHint formatHint, params CID[] columnIds)
        {
            foreach (var columnId in columnIds) dict[columnId] = formatHint;
            return dict;
        }
    }
}
