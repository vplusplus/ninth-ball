
namespace NinthBall.Outputs
{
    internal static partial class ColumnDefinitions
    {
        /// <summary>
        /// Default columns presented in html output if not configured.
        /// </summary>
        public static readonly IReadOnlyList<CID> DefaultHtmlColumns =
        [
            CID.Year, CID.Age,
            CID.JanPreTax, CID.JanPostTax, CID.JanValue,

            CID.Fees, 
            CID.PYTaxes,
            CID.LivExp,

            CID.SS,
            CID.Ann,

            CID.XPreTax, CID.XPostTax,
            CID.ROIAmount,
            CID.DecValue,

            CID.LikeYear, CID.ROI, CID.AnnROI, CID.ROIStocks, CID.ROIBonds
        ];


        /// <summary>
        /// Default columns presented in Excel output if not configured.
        /// </summary>
        public static readonly IReadOnlyList<CID> DefaultExcelColumns =
        [
            CID.Year, CID.Age,
            CID.JanPreTax, CID.JanPostTax, CID.JanValue,
            CID.DecValue,
            CID.LikeYear, CID.ROI, CID.ROIStocks, CID.ROIBonds
        ];

    }
}
