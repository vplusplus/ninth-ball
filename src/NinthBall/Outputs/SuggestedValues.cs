
using NinthBall.Core;
using System;
using System.Collections.Generic;
using System.Text;


namespace NinthBall.Outputs
{
    internal static partial class ColumnDefinitions
    {
        internal static double? GetCellValue(this SimYear simYear, CID cid) => FxValues.TryGetValue(cid, out var fxValue) && null != fxValue ? fxValue(simYear) : null;


        delegate double ValueSelector(SimYear simYear);
        delegate double SumValueSelector(SimIteration simIeration);

        static readonly IReadOnlyDictionary<CID, ValueSelector> FxValues = new Dictionary<CID, ValueSelector>()
        {
            [CID.Year] = y => y.Year + 1,
            [CID.Age] = y => y.Age,

            // Asset values at start
            [CID.JanTotal] = y => y.Jan.Total(),
            [CID.JanValue] = y => y.Jan.ApproxValue,
            [CID.JanPreTax] = y => y.Jan.PreTax.Amount,
            [CID.JanPostTax] = y => y.Jan.PostTax.Amount,
            [CID.JanCash] = y => y.Jan.Cash.Amount,
            [CID.JanPreTaxAlloc] = y => y.Jan.PreTax.Allocation,
            [CID.JanPostTaxAlloc] = y => y.Jan.PostTax.Allocation,

            [CID.Fees] = y => y.Fees.Total(),
            [CID.PYTaxes] = y => y.Expenses.PYTax.Total(),
            [CID.CYExp]   = y => y.Expenses.CYExp,

            [CID.SS] = y => y.Incomes.SS,
            [CID.Ann] = y => y.Incomes.Ann,
            [CID.XPreTax] = y => y.XPreTax,
            [CID.XPostTax] = y => y.XPostTax,
            [CID.XCash] = y => y.XCash,
            [CID.Change] = y => y.Change.Total(),

            [CID.DecTotal] = y => y.Dec.Total(),
            [CID.DecValue] = y => y.Dec.ApproxValue,
            [CID.DecPreTax] = y => y.Dec.PreTax.Amount,
            [CID.DecPostTax] = y => y.Dec.PostTax.Amount,
            [CID.DecCash] = y => y.Dec.Cash.Amount,
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
            [CID.Year] = iter => iter.Max(y => y.Year + 1),
            [CID.Age] = iter => iter.Max(y => y.Age),

            [CID.JanTotal] = iter => iter.Sum(y => y.Jan.Total()),
            [CID.JanValue] = iter => iter.Sum(y => y.Jan.ApproxValue),
            [CID.DecTotal] = iter => iter.Sum(y => y.Dec.Total()),
            [CID.DecValue] = iter => iter.Sum(y => y.Dec.ApproxValue),

        }.AsReadOnly();
    }
}
