
using NinthBall.Core;

namespace NinthBall.Outputs
{
    internal static partial class ColumnDefinitions
    {
        private delegate double ValueSelector(SimIteration iteration, in SimYear simYear);

        private delegate double SumValueSelector(SimIteration simIeration);

        /// <summary>
        /// Retrieves the value of a specific cell for a given year and column.
        /// The cell value as a double, or null if the column is not defined or applicable.
        /// </summary>
        internal static double? GetCellValue(this SimYear simYear, CID cid, SimIteration iter) => 
            FxValues.TryGetValue(cid, out var fxValue) && null != fxValue 
                ? fxValue(iter, in simYear) 
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
            [CID.Year]            = (SimIteration it, in SimYear y) => y.Year + 1,
            [CID.Age]             = (SimIteration it, in SimYear y) => y.Age,

            // Asset values at start
            [CID.JanTotal]        = (SimIteration it, in SimYear y) => y.Jan.Total(),
            [CID.JanValue]        = (SimIteration it, in SimYear y) => y.Jan.ApproxValue,
            [CID.JanPreTax]       = (SimIteration it, in SimYear y) => y.Jan.PreTax.Amount,
            [CID.JanPostTax]      = (SimIteration it, in SimYear y) => y.Jan.PostTax.Amount,
            [CID.JanCash]         = (SimIteration it, in SimYear y) => y.Jan.Cash.Amount,
            [CID.JanPreTaxAlloc]  = (SimIteration it, in SimYear y) => y.Jan.PreTax.Allocation,
            [CID.JanPostTaxAlloc] = (SimIteration it, in SimYear y) => y.Jan.PostTax.Allocation,

            [CID.Fees]            = (SimIteration it, in SimYear y) => y.Fees.Total(),
            [CID.TaxOrdIncome]    = (SimIteration it, in SimYear y) => y.Expenses.PYTax.OrdIncomeTax,
            [CID.TaxDiv]          = (SimIteration it, in SimYear y) => y.Expenses.PYTax.DividendsTax,
            [CID.TaxInt]          = (SimIteration it, in SimYear y) => y.Expenses.PYTax.InterestsTax,
            [CID.TaxCapGains]     = (SimIteration it, in SimYear y) => y.Expenses.PYTax.CapGainTax,
            [CID.PYTaxes]         = (SimIteration it, in SimYear y) => y.Expenses.PYTax.Total(),
            [CID.CYExp]           = (SimIteration it, in SimYear y) => y.Expenses.CYExp,

            [CID.Incomes]         = (SimIteration it, in SimYear y) => y.Incomes.Total(),
            [CID.SS]              = (SimIteration it, in SimYear y) => y.Incomes.SS,
            [CID.Ann]             = (SimIteration it, in SimYear y) => y.Incomes.Ann,
            [CID.XPreTax]         = (SimIteration it, in SimYear y) => y.XPreTax,
            [CID.XPostTax]        = (SimIteration it, in SimYear y) => y.XPostTax,
            [CID.XCash]           = (SimIteration it, in SimYear y) => y.XCash,
            [CID.Change]          = (SimIteration it, in SimYear y) => y.Change.Total(),

            [CID.DecTotal]        = (SimIteration it, in SimYear y) => y.Dec.Total(),
            [CID.DecValue]        = (SimIteration it, in SimYear y) => y.Dec.ApproxValue,
            [CID.DecPreTax]       = (SimIteration it, in SimYear y) => y.Dec.PreTax.Amount,
            [CID.DecPostTax]      = (SimIteration it, in SimYear y) => y.Dec.PostTax.Amount,
            [CID.DecCash]         = (SimIteration it, in SimYear y) => y.Dec.Cash.Amount,
            [CID.DecPreTaxAlloc]  = (SimIteration it, in SimYear y) => y.Dec.PreTax.Allocation,
            [CID.DecPostTaxAlloc] = (SimIteration it, in SimYear y) => y.Dec.PostTax.Allocation,

            [CID.LikeYear]        = (SimIteration it, in SimYear y) => y.ROI.LikeYear,
            [CID.ROIStocks]       = (SimIteration it, in SimYear y) => y.ROI.StocksROI,
            [CID.ROIBonds]        = (SimIteration it, in SimYear y) => y.ROI.BondsROI,
            [CID.ROICash]         = (SimIteration it, in SimYear y) => y.ROI.CashROI,
            [CID.ROI]             = (SimIteration it, in SimYear y) => y.EffectiveROI,

        }.AsReadOnly();

        static readonly IReadOnlyDictionary<CID, SumValueSelector> FxAggregates = new Dictionary<CID, SumValueSelector>()
        {
            [CID.Year]   = iter => iter.SurvivedYears,
            [CID.Age]    = iter => iter.SurvivedAge,

            // Asset values at last good sim year
            [CID.JanTotal]   = iter => iter.LastGoodYear.Jan.Total(),
            [CID.JanValue]   = iter => iter.LastGoodYear.Jan.ApproxValue,
            [CID.JanPreTax]  = iter => iter.LastGoodYear.Jan.PreTax.Amount,
            [CID.JanPostTax] = iter => iter.LastGoodYear.Jan.PostTax.Amount,
            [CID.JanCash]    = iter => iter.LastGoodYear.Jan.Cash.Amount,

            [CID.Fees]       = iter => iter.Sum(y => y.Fees.Total()),
            [CID.TaxOrdIncome] = iter => iter.Sum(y => y.Expenses.PYTax.OrdIncomeTax),
            [CID.TaxDiv]       = iter => iter.Sum(y => y.Expenses.PYTax.DividendsTax),
            [CID.TaxInt]       = iter => iter.Sum(y => y.Expenses.PYTax.InterestsTax),
            [CID.TaxCapGains]  = iter => iter.Sum(y => y.Expenses.PYTax.CapGainTax),
            [CID.PYTaxes]    = iter => iter.Sum(y => y.Expenses.PYTax.Total()),
            [CID.CYExp]      = iter => iter.Sum(y => y.Expenses.CYExp),

            [CID.Incomes]    = iter => iter.Sum(y => y.Incomes.Total()),
            [CID.SS]         = iter => iter.Sum(y => y.Incomes.SS),
            [CID.Ann]        = iter => iter.Sum(y => y.Incomes.Ann),
            [CID.XPreTax]    = iter => iter.Sum(y => y.XPreTax),
            [CID.XPostTax]   = iter => iter.Sum(y => y.XPostTax),
            [CID.XCash]      = iter => iter.Sum(y => y.XCash),
            [CID.Change]     = iter => iter.Sum(y => y.Change.Total()),

            [CID.DecTotal]   = iter => iter.LastGoodYear.Dec.Total(),
            [CID.DecValue]   = iter => iter.LastGoodYear.Dec.ApproxValue,
            [CID.DecPreTax]  = iter => iter.LastGoodYear.Dec.PreTax.Amount,
            [CID.DecPostTax] = iter => iter.LastGoodYear.Dec.PostTax.Amount,
            [CID.DecCash]    = iter => iter.LastGoodYear.Dec.Cash.Amount,

            [CID.ROIStocks]  = iter => iter.Annualize(y => y.ROI.StocksROI),
            [CID.ROIBonds]   = iter => iter.Annualize(y => y.ROI.BondsROI),
            [CID.ROICash]    = iter => iter.Annualize(y => y.ROI.CashROI),
            [CID.ROI]        = iter => iter.Annualize(y => y.EffectiveROI),

        }.AsReadOnly();
    }
}
