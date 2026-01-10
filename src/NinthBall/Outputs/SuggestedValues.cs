
using NinthBall.Core;

namespace NinthBall.Outputs
{
    internal static partial class ColumnDefinitions
    {
        private delegate double ValueSelector(SimIteration iteration, SimYear simYear);

        private delegate double SumValueSelector(SimIteration simIeration);

        /// <summary>
        /// Retrieves the value of a specific cell for a given year and column.
        /// The cell value as a double, or null if the column is not defined or applicable.
        /// </summary>
        internal static double? GetCellValue(this SimYear simYear, CID cid, SimIteration iter) => 
            FxValues.TryGetValue(cid, out var fxValue) && null != fxValue 
                ? fxValue(iter, simYear) 
                : null;

        /// <summary>
        /// Retrieves an aggregated value (e.g., Sum, Max) for a specific column across the entire iteration.
        /// The aggregation method (Sum, Max, etc.) is predefined for each column ID.
        /// The aggregated value as a double, or null if no aggregation is defined for this column.
        /// </summary>
        internal static double? GetAggregateValue(this SimIteration iter, CID cid) =>
            FxAggregates.TryGetValue(cid, out var fxValue) && null != fxValue
                ? fxValue(iter)
                : null;

        static readonly IReadOnlyDictionary<CID, ValueSelector> FxValues = new Dictionary<CID, ValueSelector>()
        {
            [CID.Year]            = (it, y) => y.Year + 1,
            [CID.Age]             = (it, y) => y.Age,

            // Asset values at start
            [CID.JanTotal]        = (it, y) => y.Jan.Total(),
            [CID.JanValue]        = (it, y) => y.Jan.ApproxValue,
            [CID.JanPreTax]       = (it, y) => y.Jan.PreTax.Amount,
            [CID.JanPostTax]      = (it, y) => y.Jan.PostTax.Amount,
            [CID.JanCash]         = (it, y) => y.Jan.Cash.Amount,
            [CID.JanPreTaxAlloc]  = (it, y) => y.Jan.PreTax.Allocation,
            [CID.JanPostTaxAlloc] = (it, y) => y.Jan.PostTax.Allocation,

            [CID.Fees]            = (it, y) => y.Fees.Total(),
            [CID.PYTaxes]         = (it, y) => y.Expenses.PYTax.Total(),
            [CID.CYExp]           = (it, y) => y.Expenses.CYExp,

            [CID.SS]              = (it, y) => y.Incomes.SS,
            [CID.Ann]             = (it, y) => y.Incomes.Ann,
            [CID.XPreTax]         = (it, y) => y.XPreTax,
            [CID.XPostTax]        = (it, y) => y.XPostTax,
            [CID.XCash]           = (it, y) => y.XCash,
            [CID.Change]          = (it, y) => y.Change.Total(),

            [CID.DecTotal]        = (it, y) => y.Dec.Total(),
            [CID.DecValue]        = (it, y) => y.Dec.ApproxValue,
            [CID.DecPreTax]       = (it, y) => y.Dec.PreTax.Amount,
            [CID.DecPostTax]      = (it, y) => y.Dec.PostTax.Amount,
            [CID.DecCash]         = (it, y) => y.Dec.Cash.Amount,
            [CID.DecPreTaxAlloc]  = (it, y) => y.Dec.PreTax.Allocation,
            [CID.DecPostTaxAlloc] = (it, y) => y.Dec.PostTax.Allocation,

            [CID.LikeYear]        = (it, y) => y.ROI.LikeYear,
            [CID.ROIStocks]       = (it, y) => y.ROI.StocksROI,
            [CID.ROIBonds]        = (it, y) => y.ROI.BondsROI,
            [CID.ROICash]         = (it, y) => y.ROI.CashROI,
            [CID.ROI]             = (it, y) => y.EffectiveROI,

        }.AsReadOnly();

        static readonly IReadOnlyDictionary<CID, SumValueSelector> FxAggregates = new Dictionary<CID, SumValueSelector>()
        {
            [CID.Year]     = iter => iter.Max(y => y.Year + 1),
            [CID.Age]      = iter => iter.Max(y => y.Age),

            [CID.JanTotal] = iter => iter.Sum(y => y.Jan.Total()),
            [CID.JanValue] = iter => iter.Sum(y => y.Jan.ApproxValue),
            
            [CID.Fees]     = iter => iter.Sum(y => y.Fees.Total()),
            [CID.PYTaxes]  = iter => iter.Sum(y => y.Expenses.PYTax.Total()),
            [CID.CYExp]    = iter => iter.Sum(y => y.Expenses.CYExp),

            [CID.SS]       = iter => iter.Sum(y => y.Incomes.SS),
            [CID.Ann]      = iter => iter.Sum(y => y.Incomes.Ann),
            [CID.XPreTax]  = iter => iter.Sum(y => y.XPreTax),
            [CID.XPostTax] = iter => iter.Sum(y => y.XPostTax),
            [CID.XCash]    = iter => iter.Sum(y => y.XCash),
            [CID.Change]   = iter => iter.Sum(y => y.Change.Total()),

            [CID.DecTotal] = iter => iter.Sum(y => y.Dec.Total()),
            [CID.DecValue] = iter => iter.Sum(y => y.Dec.ApproxValue),

        }.AsReadOnly();
    }
}
