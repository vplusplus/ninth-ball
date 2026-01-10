
using NinthBall.Core;

using static NinthBall.Outputs.FormatHint;
using static NinthBall.Outputs.WidthHint;
using static NinthBall.Outputs.AlignHint;
using static NinthBall.Outputs.ColorHint;


namespace NinthBall.Outputs
{
    internal enum FormatHint { F0, C0, C1, C2, P0, P1, P2 }
    internal enum WidthHint  { W1x, W2x, W3x, W4x }
    internal enum AlignHint  { Left, Center, Right }
    internal enum ColorHint  { None, Success, Warning, Danger, Muted }
    internal enum CID
    {
        NA, Empty,
        Year, Age,
        Jan, JanValue, JanPreTax, JanPreTaxAlloc, JanPostTax, JanPostTaxAlloc, JanCash, 
        Fees,
        TaxOrdIncome, TaxDiv, TaxInt, TaxCapGains, Tax,
        CYExp,
        SS, Ann, Incomes,
        XPreTax, XPostTax, XCash, XTotal,
        Dec, DecValue, DecPreTax, DecPreTaxAlloc, DecPostTax, DecPostTaxAlloc, DecCash, DecTotal, DecApprox,
        LikeYear, ROI, ROIStocks, ROIBonds, ROICash,
    }

    internal static class ColumnDefinitions
    {
        public static readonly IReadOnlyList<CID> DefaultColumns =
        [
            CID.Year, CID.Age,
            CID.JanPreTax, CID.JanPostTax, CID.JanValue,
            CID.DecValue,
            CID.LikeYear, CID.ROI, CID.ROIStocks, CID.ROIBonds
        ];

        internal static WidthHint GetWidthHint(this CID cid) => Widths.TryGetValue(cid, out var widthHint) ? widthHint : W2x;
        internal static AlignHint GetAlignmentHint(this CID cid) => Alignments.TryGetValue(cid, out var alignHint) ? alignHint : AlignHint.Right;
        internal static FormatHint GetFormatHint(this CID cid) => Formats.TryGetValue(cid, out var hint) ? hint : FormatHint.C0;
        internal static string GetColName(this CID cid) => ColumnNames.TryGetValue(cid, out var colName) && null != colName ? colName : cid.ToString();
        internal static string GetColTitle(this CID cid) => ColumnTitles.TryGetValue(cid, out var colTitle) && null != colTitle ? colTitle : string.Empty;
        internal static double? GetCellValue(this SimYear simYear, CID cid) => FxValues.TryGetValue(cid, out var fxValue) && null != fxValue ? fxValue(simYear) : null;
        internal static ColorHint GetCellColorHint(this SimYear y, CID cid, SimIteration iter) => FxColors.TryGetValue(cid, out var fxColor) && null != fxColor ? fxColor(iter, y) : ColorHint.None;




        delegate ColorHint ColorSelector(SimIteration iteration, SimYear year);
        delegate double ValueSelector(SimYear simYear);
        delegate double SumValueSelector(SimIteration simIeration);


        static readonly IReadOnlyDictionary<CID, ValueSelector> FxValues = new Dictionary<CID, ValueSelector>()
        {
            [CID.Year] = y => y.Year + 1,
            [CID.Age] = y => y.Age,

            [CID.Jan] = y => y.Jan.Total(),
            [CID.JanValue] = y => y.Jan.ApproxValue,
            [CID.JanPreTax] = y => y.Jan.PreTax.Amount,
            [CID.JanPostTax] = y => y.Jan.PostTax.Amount,
            [CID.JanCash] = y => y.Jan.Cash.Amount,

            [CID.Dec] = y => y.Dec.Total(),
            [CID.DecValue] = y => y.Dec.ApproxValue,
            [CID.DecPreTax] = y => y.Dec.PreTax.Amount,
            [CID.DecPostTax] = y => y.Dec.PostTax.Amount,
            [CID.DecCash] = y => y.Dec.Cash.Amount,

            [CID.JanPreTaxAlloc] = y => y.Jan.PreTax.Allocation,
            [CID.JanPostTaxAlloc] = y => y.Jan.PostTax.Allocation,
            [CID.DecPreTaxAlloc] = y => y.Dec.PreTax.Allocation,
            [CID.DecPostTaxAlloc] = y => y.Dec.PostTax.Allocation,


            [CID.LikeYear] = y => y.ROI.LikeYear,
            [CID.ROIStocks] = y => y.ROI.StocksROI,
            [CID.ROIBonds] = y => y.ROI.BondsROI,
            [CID.ROICash] = y => y.ROI.CashROI,
            [CID.ROI] = y => y.EffectiveROI,

        }.AsReadOnly();

        static readonly IReadOnlyDictionary<CID, SumValueSelector> FxAggregates = new Dictionary<CID, SumValueSelector>()
        {
            [CID.Year]      = iter => iter.Max(y => y.Year + 1),
            [CID.Age]       = iter => iter.Max(y => y.Age),

            [CID.Jan]       = iter => iter.Sum(y => y.Jan.Total()),
            [CID.JanValue]  = iter => iter.Sum(y => y.Jan.ApproxValue),
            [CID.Dec]       = iter => iter.Sum(y => y.Dec.Total()),
            [CID.DecValue]  = iter => iter.Sum(y => y.Dec.ApproxValue),

        }.AsReadOnly();

        static readonly IReadOnlyDictionary<CID, FormatHint> Formats = new Dictionary<CID, FormatHint>()
        {
            [CID.Year] = F0,
            [CID.Age]  = F0,

            [CID.JanPreTaxAlloc] = P0,
            [CID.JanPostTaxAlloc] = P0,
            [CID.DecPreTaxAlloc] = P0,
            [CID.DecPostTaxAlloc] = P0,

            [CID.LikeYear]  = F0,
            [CID.ROIStocks] = P1,
            [CID.ROIBonds]  = P1,
            [CID.ROICash]   = P1,
            [CID.ROI]       = P1,

        }.AsReadOnly();

        static readonly IReadOnlyDictionary<CID, AlignHint> Alignments = new Dictionary<CID, AlignHint>()
        {
            [CID.Year] = Center,
            [CID.Age] = Center,
            //[CID.LikeYear] = Center,

        }.AsReadOnly();

        static readonly IReadOnlyDictionary<CID, WidthHint> Widths = new Dictionary<CID, WidthHint>()
        {
            [CID.Year]      = W1x,
            [CID.Age]       = W1x,

            [CID.LikeYear]  = W1x,
            [CID.ROI]       = W1x,
            [CID.ROIStocks] = W1x,
            [CID.ROIBonds]  = W1x,
            [CID.ROICash]   = W1x,

        }.AsReadOnly();

        static readonly IReadOnlyDictionary<CID, ColorSelector> FxColors = new Dictionary<CID, ColorSelector>()
        {
            [CID.LikeYear]  = (it, y) => ROIRedGreyGreen(y),
            [CID.ROI]       = (it, y) => ROIRedGreyGreen(y),
            [CID.ROIStocks] = (it, y) => ROIRedGreyGreen(y.ROI.StocksROI),
            [CID.ROIBonds]  = (it, y) => ROIRedGreyGreen(y.ROI.BondsROI),
            [CID.ROICash]   = (it, y) => ROIRedGreyGreen(y.ROI.CashROI),

        }.AsReadOnly();

        static readonly IReadOnlyDictionary<CID, string> ColumnNames = new Dictionary<CID, string>()
        {
            [CID.Year] = "Yr",
            
            [CID.JanValue]    = "~Value",
            [CID.DecValue]    = "~Value",

            [CID.JanPreTax]   = "Jan 401K",
            [CID.JanPostTax]  = "Jan Inv",
            [CID.DecPreTax]   = "Dec 401K",
            [CID.DecPostTax]  = "Dec Inv",

            [CID.LikeYear]    = "Like",
            [CID.ROI]         = "ROI",
            [CID.ROIStocks]   = "Stocks",
            [CID.ROIBonds]    = "Bonds",

        }.AsReadOnly();

        static readonly IReadOnlyDictionary<CID, string> ColumnTitles = new Dictionary<CID, string>()
        {
            [CID.ROI] = "Effective ROI (StocAlloc x StockROI + BondAlloc x BondROI)",
            [CID.JanValue] = "Approx value (401K x 75% + Inv x 85%)",
            [CID.DecValue] = "Approx value (401K x 75% + Inv x 85%)"

        }.AsReadOnly();


        static ColorHint ROIRedGreyGreen(SimYear simYear) => ROIRedGreyGreen(simYear.EffectiveROI);
        static ColorHint ROIRedGreyGreen(double pctValue) => pctValue >= -0.04 && pctValue <= +0.04 ? ColorHint.Muted : pctValue <= 0 ? ColorHint.Danger : ColorHint.Success;


       

    }
}
