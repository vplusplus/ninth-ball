
namespace NinthBall.Reports
{
    internal static partial class Suggested
    {
        internal static string GetColName(this CID cid) => ColumnNames.TryGetValue(cid, out var colName) && null != colName ? colName : cid.ToString();

        static readonly IReadOnlyDictionary<CID, string> ColumnNames = new Dictionary<CID, string>()
        {
            [CID.Year]            = "Yr",

            [CID.JanValue]        = "~Jan",
            [CID.DecValue]        = "~Dec",

            [CID.JanPreTax]       = "Jan 401K",
            [CID.JanPostTax]      = "Jan Inv",

            [CID.DecPreTax]       = "Dec 401K",
            [CID.DecPostTax]      = "Dec Inv",

            [CID.LikeYear]        = "Like",
            [CID.ROI]             = "ROI",
            [CID.ROIStocks]       = "Stocks",
            [CID.ROIBonds]        = "Bonds",
            [CID.InflationRate]   = "Inf",
            [CID.AnnROI]          = "AnnROI",
            [CID.RealCAGR]        = "RealCAGR",

            [CID.JanPreTaxAlloc]  = "STK%",
            [CID.JanPostTaxAlloc] = "STK%",
            [CID.DecPreTaxAlloc]  = "STK%",
            [CID.DecPostTaxAlloc] = "STK%",


        }.AsReadOnly();
    }
}
