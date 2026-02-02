
using NinthBall.Core;

namespace NinthBall.Reports
{
    internal static class OutputValues
    {
        private delegate double ValueSelector(SimIteration iteration, in SimYear simYear);
        private delegate double SumValueSelector(SimIteration simIteration);

        /// <summary>
        /// Retrieves the value of a specific cell for a given year and column.
        /// Returns the cell value as a double, or null if the column is not defined or applicable.
        /// </summary>
        internal static double? GetCellValue(this SimYear simYear, CID cid, SimIteration iter) => 
            FxValues.TryGetValue(cid, out var fxValue) && null != fxValue 
                ? fxValue(iter, in simYear) 
                : null;

        /// <summary>
        /// Retrieves an aggregated value (e.g., Sum, Max) for a specific column across the entire iteration.
        /// The aggregation method (Sum, Max, etc.) is predefined for each column ID.
        /// Returns the aggregated value as a double, or null if no aggregation is defined for this column.
        /// </summary>
        internal static double? GetAggregateValue(this SimIteration iter, CID cid) =>
            FxAggregates.TryGetValue(cid, out var fxValue) && null != fxValue
                ? fxValue(iter)
                : null;

        static readonly IReadOnlyDictionary<CID, ValueSelector> FxValues = new Dictionary<CID, ValueSelector>()
        {
            [CID.Year]              = (it, in y) => y.Year + 1,
            [CID.Age]               = (it, in y) => y.Age,

            // Stock asset changes due to rebalancing  (Bonds changes are mirror image)
            [CID.RBL]               = (it, in y) => y.Rebalanced.StocksChange,
            [CID.RBLPreTax]         = (it, in y) => y.Rebalanced.PreTax.StocksChange,
            [CID.RBLPostTax]        = (it, in y) => y.Rebalanced.PostTax.StocksChange,

            // Total portfolio values (gross and approx after-tax-worth)
            [CID.Jan]               = (it, in y) => y.Jan.Total,
            [CID.JanNet]            = (it, in y) => y.Jan.AfterTaxNetWorth,
            [CID.Dec]               = (it, in y) => y.Dec.Total,
            [CID.DecNet]            = (it, in y) => y.Dec.AfterTaxNetWorth,

            // Balance and allocation of individual assets
            [CID.JanPreTax]         = (it, in y) => y.Jan.PreTax.Amount,
            [CID.JanPostTax]        = (it, in y) => y.Jan.PostTax.Amount,
            [CID.JanCash]           = (it, in y) => y.Jan.Cash.Amount,
            [CID.JanPreTaxAlloc]    = (it, in y) => y.Jan.PreTax.Allocation,
            [CID.JanPostTaxAlloc]   = (it, in y) => y.Jan.PostTax.Allocation,
            [CID.DecPreTax]         = (it, in y) => y.Dec.PreTax.Amount,
            [CID.DecPostTax]        = (it, in y) => y.Dec.PostTax.Amount,
            [CID.DecCash]           = (it, in y) => y.Dec.Cash.Amount,
            [CID.DecPreTaxAlloc]    = (it, in y) => y.Dec.PreTax.Allocation,
            [CID.DecPostTaxAlloc]   = (it, in y) => y.Dec.PostTax.Allocation,

            // Additional incomes
            [CID.Incomes]           = (it, in y) => y.Incomes.Total,
            [CID.SS]                = (it, in y) => y.Incomes.SS,
            [CID.Ann]               = (it, in y) => y.Incomes.Ann,

            // Expenses (there is only one)
            [CID.LivExp]            = (it, in y) => y.Expenses.LivExp,

            // Fees
            [CID.Fees]              = (it, in y) => y.Fees.Total,
            [CID.FeesPreTax]        = (it, in y) => y.Fees.PreTax,
            [CID.FeesPostTax]       = (it, in y) => y.Fees.PostTax,

            // Withdrawals and Deposits 
            [CID.XPreTax]           = (it, in y) => y.XPreTax,
            [CID.XPostTax]          = (it, in y) => y.XPostTax,
            [CID.XCash]             = (it, in y) => y.XCash,

            // Change in asset values due to ROI
            [CID.Change]            = (it, in y) => y.Change.Total,
            [CID.ChangePreTax]      = (it, in y) => y.Change.PreTax,
            [CID.ChangePostTax]     = (it, in y) => y.Change.PostTax,

            // Inflation and Growth
            [CID.LikeYear]          = (it, in y) => y.ROI.LikeYear,
            [CID.Infl]              = (it, in y) => y.ROI.InflationRate,
            [CID.ROIStocks]         = (it, in y) => y.ROI.StocksROI,
            [CID.ROIBonds]          = (it, in y) => y.ROI.BondsROI,
            [CID.ROI]               = (it, in y) => y.Metrics.PortfolioReturn,
            [CID.AnnROI]            = (it, in y) => y.Metrics.AnnualizedReturn,
            [CID.RealCAGR]          = (it, in y) => y.Metrics.RealAnnualizedReturn,

            // Tax $$$s, MTR and effective tax rate
            [CID.Taxes]             = (it, in y) => y.Taxes.Total,
            [CID.TaxPCT]            = (it, in y) => y.Taxes.TaxPCT,

            // Marginal tax rates
            [CID.MTROrdInc]         = (it, in y) => y.Taxes.Federal.MTR,
            [CID.MTRCapGain]        = (it, in y) => y.Taxes.Federal.MTRCapGain,
            [CID.MTRState]          = (it, in y) => y.Taxes.State.MTR,

            // All about uncle Sam
            [CID.FedAGI]            = (it, in y) => y.Taxes.Federal.Gross,
            [CID.FedDeduct]         = (it, in y) => y.Taxes.Federal.Deductions,
            [CID.FedTaxable]        = (it, in y) => y.Taxes.Federal.Taxable,
            [CID.FedTax]            = (it, in y) => y.Taxes.Federal.Tax,
            [CID.FedTaxPCT]         = (it, in y) => y.Taxes.TaxPCTFed,

            // All about uncle Sam's brother
            [CID.StaAGI]            = (it, in y) => y.Taxes.State.Gross,
            [CID.StaDeduct]         = (it, in y) => y.Taxes.State.Deductions,
            [CID.StaTaxable]        = (it, in y) => y.Taxes.State.Taxable,
            [CID.StaTax]            = (it, in y) => y.Taxes.State.Tax,
            [CID.StaTaxPCT]         = (it, in y) => y.Taxes.TaxPCTState,

            // Unadjusted gross income from all sources
            [CID.GI]                = (it, in y) => y.Taxes.GrossIncome.Total,
            [CID.GIPreTax]          = (it, in y) => y.Taxes.GrossIncome.PreTaxWDraw,
            [CID.GISS]              = (it, in y) => y.Taxes.GrossIncome.SS,
            [CID.GIAnn]             = (it, in y) => y.Taxes.GrossIncome.Ann,
            [CID.GIBonds]           = (it, in y) => y.Taxes.GrossIncome.BondsYield,
            [CID.GIDiv]             = (it, in y) => y.Taxes.GrossIncome.Dividends,
            [CID.GICapGain]         = (it, in y) => y.Taxes.GrossIncome.CapGains,

            // Running multipliers
            [CID.MXInf]             = (it, in y) => y.Metrics.InflationMultiplier,
            [CID.MXInfFedTax]       = (it, in y) => y.Metrics.FedTaxInflationMultiplier,
            [CID.MXInfStaTax]       = (it, in y) => y.Metrics.StateTaxInflationMultiplier,
            [CID.MXGrowth]          = (it, in y) => y.Metrics.GrowthMultiplier,


        }.AsReadOnly();

        static readonly IReadOnlyDictionary<CID, SumValueSelector> FxAggregates = new Dictionary<CID, SumValueSelector>()
        {
            [CID.Year]         = (it) => it.SurvivedYears,

            // Aggregated version presents the last good year info
            [CID.Jan]     = (it) => it.LastGoodYear.Jan.Total,
            [CID.JanNet]     = (it) => it.LastGoodYear.Jan.AfterTaxNetWorth,
            [CID.JanPreTax]    = (it) => it.LastGoodYear.Jan.PreTax.Amount,
            [CID.JanPostTax]   = (it) => it.LastGoodYear.Jan.PostTax.Amount,
            [CID.JanCash]      = (it) => it.LastGoodYear.Jan.Cash.Amount,

            [CID.Fees]         = (it) => it.Sum(y => y.Fees.Total),
            [CID.Taxes]      = (it) => it.Sum(y => y.Taxes.Total),

            [CID.LivExp]       = (it) => it.Sum(y => y.Expenses.LivExp),

            [CID.Incomes]      = (it) => it.Sum(y => y.Incomes.Total),
            [CID.SS]           = (it) => it.Sum(y => y.Incomes.SS),
            [CID.Ann]          = (it) => it.Sum(y => y.Incomes.Ann),
            [CID.XPreTax]      = (it) => it.Sum(y => y.XPreTax),
            [CID.XPostTax]     = (it) => it.Sum(y => y.XPostTax),
            [CID.XCash]        = (it) => it.Sum(y => y.XCash),


            // Bottom-line: Data is nominal. Show nominal value of last good year.
            [CID.Dec]     = (it) => it.LastGoodYear.Dec.Total,
            [CID.DecNet]     = (it) => it.LastGoodYear.Dec.AfterTaxNetWorth,
            [CID.DecPreTax]    = (it) => it.LastGoodYear.Dec.PreTax.Amount,
            [CID.DecPostTax]   = (it) => it.LastGoodYear.Dec.PostTax.Amount,
            [CID.DecCash]      = (it) => it.LastGoodYear.Dec.Cash.Amount,

            // Bottom-line: Show annualized-effective-roi at last good year for both ROI and AnnROI
            // Do not try to summarize the market noise: StocksROI & BondROI - They are just bootstrapper data.
            [CID.ROI]          = (it) => it.LastGoodYear.Metrics.PortfolioReturn,
            [CID.AnnROI]       = (it) => it.LastGoodYear.Metrics.AnnualizedReturn,
            [CID.RealCAGR]     = (it) => it.LastGoodYear.Metrics.RealAnnualizedReturn,


        }.AsReadOnly();
    }
}

