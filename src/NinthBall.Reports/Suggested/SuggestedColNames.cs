
namespace NinthBall.Reports
{
    internal static partial class Suggested
    {
        internal static string GetColName(this CID cid) => ColumnNames.TryGetValue(cid, out var colName) && null != colName ? colName : cid.ToString();

        static readonly IReadOnlyDictionary<CID, string> ColumnNames = new Dictionary<CID, string>()
        {
            [CID.Year]              = "Yr",
            [CID.LikeYear]          = "Like",

            [CID.JanPreTax]         = "Jan-401K",
            [CID.JanPostTax]        = "Jan-Inv",
            [CID.DecPreTax]         = "Dec-401K",
            [CID.DecPostTax]        = "Dec-Inv",
            [CID.JanPreTaxAlloc]    = "401K[S%]",
            [CID.JanPostTaxAlloc]   = "INV[S%]",


            [CID.Change]            = "(±)",
            [CID.ChangePreTax]      = "PreTax(±)",
            [CID.ChangePostTax]     = "PostTax(±)",

            [CID.RealCAGR]          = "CAGR(r)",


        }.AsReadOnly();
    }
}
