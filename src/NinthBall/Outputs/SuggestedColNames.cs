using System;
using System.Collections.Generic;
using System.Text;

namespace NinthBall.Outputs
{
    internal static partial class ColumnDefinitions
    {
        internal static string GetColName(this CID cid) => ColumnNames.TryGetValue(cid, out var colName) && null != colName ? colName : cid.ToString();
        internal static string GetColTitle(this CID cid) => ColumnTitles.TryGetValue(cid, out var colTitle) && null != colTitle ? colTitle : string.Empty;

        static readonly IReadOnlyDictionary<CID, string> ColumnNames = new Dictionary<CID, string>()
        {
            [CID.Year]          = "Yr",

            [CID.JanValue]      = "~Value",
            [CID.DecValue]      = "~Value",

            [CID.JanPreTax]     = "Jan 401K",
            [CID.JanPostTax]    = "Jan Inv",
            [CID.DecPreTax]     = "Dec 401K",
            [CID.DecPostTax]    = "Dec Inv",

            [CID.LikeYear]      = "Like",
            [CID.ROI]           = "ROI",
            [CID.ROIStocks]     = "Stocks",
            [CID.ROIBonds]      = "Bonds",
            [CID.AnnROI]        = "AnnROI",

            [CID.JanPreTaxAlloc]   = "",
            [CID.JanPostTaxAlloc]  = "",
            [CID.DecPreTaxAlloc]   = "",
            [CID.DecPostTaxAlloc]  = "",


        }.AsReadOnly();

        static readonly IReadOnlyDictionary<CID, string> ColumnTitles = new Dictionary<CID, string>()
        {
            [CID.ROI]           = "Effective ROI (StocAlloc x StockROI + BondAlloc x BondROI)",
            [CID.JanValue]      = "Approx value (401K x 75% + Inv x 85%)",
            [CID.DecValue]      = "Approx value (401K x 75% + Inv x 85%)",
            [CID.AnnROI]        = "Cumulative Annualized (Geometric Mean) ROI from year 0"

        }.AsReadOnly();

    }
}
