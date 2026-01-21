
using NinthBall.Core;

namespace NinthBall.Outputs
{
    internal static class OutputValues
    {
        private delegate double ValueSelector(SimIteration iteration, in SimYear simYear);

        private delegate double SumValueSelector(SimIteration simIteration);

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
            [CID.JanTotal]        = (it, in y) => y.Jan.Total,
            [CID.JanValue]        = (it, in y) => y.Jan.ApproxValue,
            [CID.JanPreTax]       = (it, in y) => y.Jan.PreTax.Amount,
            [CID.JanPostTax]      = (it, in y) => y.Jan.PostTax.Amount,
            [CID.JanCash]         = (it, in y) => y.Jan.Cash.Amount,
            [CID.JanPreTaxAlloc]  = (it, in y) => y.Jan.PreTax.Allocation,
            [CID.JanPostTaxAlloc] = (it, in y) => y.Jan.PostTax.Allocation,

            [CID.Fees]            = (it, in y) => y.Fees.Total,
            [CID.PYTaxes]         = (it, in y) => y.Taxes.Total,
            [CID.LivExp]          = (it, in y) => y.Expenses.LivExp,

            [CID.Incomes]         = (it, in y) => y.Incomes.Total,
            [CID.SS]              = (it, in y) => y.Incomes.SS,
            [CID.Ann]             = (it, in y) => y.Incomes.Ann,
            [CID.XPreTax]         = (it, in y) => y.XPreTax,
            [CID.XPostTax]        = (it, in y) => y.XPostTax,
            [CID.XCash]           = (it, in y) => y.XCash,
            [CID.ROIAmount]          = (it, in y) => y.Change.Total,

            [CID.DecTotal]        = (it, in y) => y.Dec.Total,
            [CID.DecValue]        = (it, in y) => y.Dec.ApproxValue,
            [CID.DecPreTax]       = (it, in y) => y.Dec.PreTax.Amount,
            [CID.DecPostTax]      = (it, in y) => y.Dec.PostTax.Amount,
            [CID.DecCash]         = (it, in y) => y.Dec.Cash.Amount,
            [CID.DecPreTaxAlloc]  = (it, in y) => y.Dec.PreTax.Allocation,
            [CID.DecPostTaxAlloc] = (it, in y) => y.Dec.PostTax.Allocation,

            [CID.LikeYear]        = (it, in y) => y.ROI.LikeYear,
            [CID.ROIStocks]       = (it, in y) => y.ROI.StocksROI,
            [CID.ROIBonds]        = (it, in y) => y.ROI.BondsROI,
            [CID.InflationRate]   = (it, in y) => y.ROI.InflationRate,
            [CID.ROI]             = (it, in y) => y.Metrics.PortfolioReturn,
            [CID.AnnROI]          = (it, in y) => y.Metrics.AnnualizedReturn,
            [CID.RealCAGR]        = (it, in y) => y.Metrics.RealAnnualizedReturn,


            [CID.TaxFedMarginalRate]        = (it, in y) => y.Taxes.FederalTax.MarginalRateOrdInc,
            [CID.TaxFedCapGainMarginalRate] = (it, in y) => y.Taxes.FederalTax.MarginalRateCapGain,
            [CID.TaxStateMarginalRate]      = (it, in y) => y.Taxes.StateTax.MarginalRate,
            [CID.TaxEffectiveRate]          = (it, in y) => y.Taxes.EffectiveRate,

        }.AsReadOnly();

        static readonly IReadOnlyDictionary<CID, SumValueSelector> FxAggregates = new Dictionary<CID, SumValueSelector>()
        {
            [CID.Year]         = (it) => it. SurvivedYears,

            // Aggregated version presents the last good year info
            [CID.JanTotal]     = (it) => it. LastGoodYear.Jan.Total,
            [CID.JanValue]     = (it) => it. LastGoodYear.Jan.ApproxValue,
            [CID.JanPreTax]    = (it) => it. LastGoodYear.Jan.PreTax.Amount,
            [CID.JanPostTax]   = (it) => it. LastGoodYear.Jan.PostTax.Amount,
            [CID.JanCash]      = (it) => it. LastGoodYear.Jan.Cash.Amount,

            [CID.Fees]         = (it) => it. Sum(y => y.Fees.Total),
            [CID.PYTaxes]      = (it) => it. Sum(y => y.Taxes.Total),

            [CID.LivExp]       = (it) => it. Sum(y => y.Expenses.LivExp),

            [CID.Incomes]      = (it) => it. Sum(y => y.Incomes.Total),
            [CID.SS]           = (it) => it. Sum(y => y.Incomes.SS),
            [CID.Ann]          = (it) => it. Sum(y => y.Incomes.Ann),
            [CID.XPreTax]      = (it) => it. Sum(y => y.XPreTax),
            [CID.XPostTax]     = (it) => it. Sum(y => y.XPostTax),
            [CID.XCash]        = (it) => it. Sum(y => y.XCash),
            [CID.ROIAmount]    = (it) => it. Sum(y => y.Change.Total),

            // Bottom-line: Data is nominal. Show nominal value of last good year.
            [CID.DecTotal]     = (it) => it. LastGoodYear.Dec.Total,
            [CID.DecValue]     = (it) => it. LastGoodYear.Dec.ApproxValue,
            [CID.DecPreTax]    = (it) => it. LastGoodYear.Dec.PreTax.Amount,
            [CID.DecPostTax]   = (it) => it. LastGoodYear.Dec.PostTax.Amount,
            [CID.DecCash]      = (it) => it. LastGoodYear.Dec.Cash.Amount,

            // Bottom-line: Show annualized-effective-roi at last good year for both ROI and AnnROI
            // Do not try to summarize the market noise: StocksROI & BondROI - They are just bootstrapper data.
            [CID.ROI]          = (it) => it.LastGoodYear.Metrics.PortfolioReturn,
            [CID.AnnROI]       = (it) => it.LastGoodYear.Metrics.AnnualizedReturn,
            [CID.RealCAGR]     = (it) => it.LastGoodYear.Metrics.RealAnnualizedReturn,


        }.AsReadOnly();
    }
}
