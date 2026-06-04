
namespace NinthBall.Reports
{
    internal static partial class Suggested
    {
        internal static string GetColName(this CID cid) => ColumnNames.TryGetValue(cid, out var colName) && null != colName ? colName : cid.ToString();

        static readonly IReadOnlyDictionary<CID, string> ColumnNames = new Dictionary<CID, string>()
        {
            [CID.Year]              = "Yr",

            [CID.JanPreTax]         = "PreTax(Jan)",
            [CID.JanPostTax]        = "PostTax(Jan)",
            [CID.DecPreTax]         = "PreTax(Dec)",
            [CID.DecPostTax]        = "PostTax(Dec)",
            [CID.JanPreTaxAlloc]    = "[S%]",
            [CID.JanPostTaxAlloc]   = "[S%]",


            [CID.Change]            = "(±)",
            [CID.ChangePreTax]      = "PreTax(±)",
            [CID.ChangePostTax]     = "PostTax(±)",

            [CID.LikeYear]          = "Like",
            [CID.ROIStocks]         = "Stocks",
            [CID.ROIBonds]          = "Bonds",
            [CID.Infl]              = "Infl.",

            [CID.CAGRNominal]       = "CAGR(n)",
            [CID.CAGRReal]          = "CAGR(r)",


        }.AsReadOnly();
    }
}
