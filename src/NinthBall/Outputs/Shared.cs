
using NinthBall.Core;

using static NinthBall.Outputs.FormatHint;
using static NinthBall.Outputs.WidthHint;
using static NinthBall.Outputs.AlignHint;
using static NinthBall.Outputs.ColorHint;

namespace NinthBall.Outputs
{
    internal enum FormatHint { F0, C0, C1, C2, P0, P1, P2 }
    internal enum WidthHint  { WDefault = 0, WSmall, WMedium, WLarge, WXLarge }
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
        LikeYear, ROIStocks, ROIBonds, ROICash,
    }

    internal static class ColumnDefinitions
    {
        delegate ColorHint ColorSelector(SimIteration iteration, SimYear year);

        static readonly IReadOnlyDictionary<CID, Func<SimYear, double>> Values = new Dictionary<CID, Func<SimYear, double>>()
        {
            [CID.Year] = y => y.Year + 1,
            [CID.Age] = y => y.Age,

            [CID.Jan] = y => y.Jan.Total(),
            [CID.JanValue] = y => y.Jan.ApproxValue(),
            [CID.JanPreTax] = y => y.Jan.PreTax.Amount,
            [CID.JanPostTax] = y => y.Jan.PostTax.Amount,
            [CID.JanCash] = y => y.Jan.Cash.Amount,

            [CID.Dec] = y => y.Dec.Total(),
            [CID.DecValue] = y => y.Dec.ApproxValue(),
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

        }.AsReadOnly();

        static readonly IReadOnlyDictionary<CID, Func<SimIteration, double>> Aggregates = new Dictionary<CID, Func<SimIteration, double>>()
        {
            [CID.Year] = c => c.Max(y => y.Year + 1),
            [CID.Age] = c => c.Max(y => y.Age),

            [CID.Jan] = c => c.Sum(y => y.Jan.Total()),
            [CID.JanValue] = c => c.Sum(y => y.Jan.ApproxValue()),
            [CID.Dec] = c => c.Sum(y => y.Dec.Total()),
            [CID.DecValue] = c => c.Sum(y => y.Dec.ApproxValue()),

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

        }.AsReadOnly();

        static readonly IReadOnlyDictionary<CID, AlignHint> Alignments = new Dictionary<CID, AlignHint>()
        {
            [CID.Year] = Center,
            [CID.Age] = Center,
            [CID.LikeYear] = Center,

        }.AsReadOnly();

        static readonly IReadOnlyDictionary<CID, WidthHint> Widths = new Dictionary<CID, WidthHint>()
        {
            [CID.Year] = WSmall,
            [CID.Age] = WSmall,

            [CID.LikeYear] = WSmall,
            [CID.ROIStocks] = WSmall,
            [CID.ROIBonds] = WSmall,
            [CID.DecCash] = WSmall,

        }.AsReadOnly();



        static double ApproxValue(this Assets assets) => (assets.PreTax.Amount * 0.75) + (assets.PostTax.Amount * 0.85) + assets.Cash.Amount;

        static double Max(this SimIteration iteration, Func<SimYear, double> fxValueSelector)
        {
            var span = iteration.ByYear.Span;
            double value = 0.0;
            for (int i = 0; i < iteration.ByYear.Length; i++) value = Math.Max(value, fxValueSelector(span[i]));
            return value;
        }

        static double Sum(this SimIteration iteration, Func<SimYear, double> fxValueSelector)
        {
            var span = iteration.ByYear.Span;
            double sumAmount = 0.0;
            for (int i = 0; i < iteration.ByYear.Length; i++) sumAmount += fxValueSelector(span[i]);
            return sumAmount;
        }


        internal static WidthHint GetWidthHint(this CID cid)
        {
            return Widths.TryGetValue(cid, out var widthHint) ? widthHint : WDefault;
        }

        internal static AlignHint GetAlignmentHint(this CID cid)
        {
            return Alignments.TryGetValue(cid, out var alignHint) ? alignHint : AlignHint.Right;
        }

        internal static FormatHint GetFormatHint(this CID cid)
        {
            return Formats.TryGetValue(cid, out var hint) ? hint : FormatHint.C0;
        }

        internal static string GetColName(this CID cid)
        {
            return cid.ToString();
        }

        internal static string GetColTitle(this CID cid)
        {
            return cid.ToString();
        }

        internal static double? GetCellValue(this SimYear simYear, CID cid)
        {
            var isDefined = Values.TryGetValue(cid, out var fxValue);
            return isDefined && null != fxValue ? fxValue(simYear) : null;
        }

        internal static ColorHint GetCellColorHint(this SimYear y, CID cid, SimIteration iter)
        {
            return ColorHint.None;
        }



        public static IReadOnlyList<CID> DefaultColumns { get; } = new CID[]
        {
            CID.Year, CID.Age,
            CID.JanPreTax, CID.JanPostTax, CID.JanValue,
            CID.DecValue,
            CID.LikeYear, CID.ROIStocks, CID.ROIBonds, CID.ROICash,
        };


    }
}




