
namespace NinthBall.Core
{
    public static class CalculatedFields
    {
        //......................................................................
        #region Calculated fields on SimInput
        //......................................................................
        extension(SimInput input)
        {
            // Nothing yet
        }

        #endregion

        //......................................................................
        #region Calculated fields on Assets
        //......................................................................
        extension(in Assets assets)
        {
            // Approximate value, less taxes (Indicative value, not exact)
            // This is not cash in hand, if you try to withdraw all amount, will push to higher tax bracket.
            // Ignoring cash basis, and loss-harvesting - Taxes are overstated.
            // PreTax  - 100% taxable at 25%
            // PostTax - Assuming all gains are long term gains, 
            public double ApproxValue => (assets.PreTax.Amount * 0.75) + (assets.PostTax.Amount * 0.85) + assets.Cash.Amount;
        }

        extension(in Asset asset)
        {
            public double StockAlloc => asset.Allocation;
            public double BondAlloc => 1.0 - asset.Allocation;
        }

        #endregion

        //......................................................................
        #region Calculated fields on SimYear
        //......................................................................
        extension(in SimYear simYear)
        {
            // Net change
            public double XPreTax   => 0 - simYear.Withdrawals.PreTax;
            public double XPostTax  => simYear.Deposits.PostTax - simYear.Withdrawals.PostTax;
            public double XCash     => simYear.Deposits.Cash - simYear.Withdrawals.Cash;

            /// <summary>
            /// Effective ROI after fees and withdrawals, before deposits.
            /// </summary>
            public double EffectiveROI
            {
                get 
                {
                    // Balance less fees and withdrawals
                    // BY-DESIGN: Cash-balance and cash-increase are ignored by design.
                    var jan = simYear.Jan;
                    var preTaxLessFeesAndWithdrawals  = jan.PreTax.Amount  - simYear.Fees.PreTax  - simYear.Withdrawals.PreTax;
                    var postTaxLessFeesAndWithdrawals = jan.PostTax.Amount - simYear.Fees.PostTax - simYear.Withdrawals.PostTax;

                    // Stock and bond balances less fees and withdrawals
                    var S1 = preTaxLessFeesAndWithdrawals  * jan.PreTax.StockAlloc;
                    var B1 = preTaxLessFeesAndWithdrawals  * jan.PreTax.BondAlloc;
                    var S2 = postTaxLessFeesAndWithdrawals * jan.PostTax.StockAlloc;
                    var B2 = postTaxLessFeesAndWithdrawals * jan.PostTax.BondAlloc;

                    // BY-DESIGN: Deposits are made at the end of the year.
                    // Deposits are withheld from ROI calculation
                    // Simulation updates deposits after calculating ROI for the year

                    // Growth
                    var rS = simYear.ROI.StocksROI;
                    var rB = simYear.ROI.BondsROI;

                    // Change and PCT change.
                    var change = (S1 * rS) + (B1 * rB) + (S2 * rS) + (B2 * rB);
                    var pctChange = change / (preTaxLessFeesAndWithdrawals + postTaxLessFeesAndWithdrawals);

                    return pctChange;
                }
            }


        }

        #endregion

        //......................................................................
        #region Calculated fields on SimIteration 
        //......................................................................
        extension(SimIteration iteration)
        {
            public SimYear FirstYear    => iteration.ByYear.Span.Length > 0 ? iteration.ByYear.Span[0]  : new();
            public SimYear LastYear     => iteration.ByYear.Span.Length > 0 ? iteration.ByYear.Span[^1] : new();
            public SimYear LastGoodYear => iteration.Success ? iteration.ByYear.Span[^1] : iteration.ByYear.Span.Length > 1 ? iteration.ByYear.Span[^2] : new();

            public double StartingBalance => iteration.FirstYear.Jan.Total();
            public double EndingBalance   => iteration.LastYear.Dec.Total();
            public int    SurvivedYears   => iteration.LastGoodYear.Year + 1;
            public int    SurvivedAge     => iteration.LastGoodYear.Age  + 1;

            /// <summary>
            /// Zero-copy extension to calculate the sum of a selected field across all years in the iteration 
            /// </summary>
            public double Sum(Func<SimYear, double> fxValueSelector, bool ignoreFailedYear = true)
            {
                ArgumentNullException.ThrowIfNull(fxValueSelector);

                var span = iteration.ByYear.Span;
                var last = ignoreFailedYear ? iteration.SurvivedYears : span.Length;

                double sumValue = 0.0;
                for (int i = 0; i < last; i++) sumValue += fxValueSelector(span[i]);
                return sumValue;
            }

            /// <summary>
            /// Computes the annualized nominal return from a sequence of periodic returns.
            /// </summary>
            public double Annualize(Func<SimYear, double> fxPercentageValueSelector, bool ignoreFailedYear = true)
            {
                ArgumentNullException.ThrowIfNull(fxPercentageValueSelector);

                var span = iteration.ByYear.Span;
                var last = ignoreFailedYear ? iteration.SurvivedYears : span.Length;

                double compoundReturn = 1;
                int count = 0;
                for (int i = 0; i < last; i++)
                {
                    //sumValue += fxValueSelector(span[i]);
                    var r = fxPercentageValueSelector(span[i]); 
                    compoundReturn *= (1 + r);
                    count++;
                }

                return (count == 0)
                    ? throw new ArgumentException("Input sequence must contain at least one value.")
                    : Math.Pow(compoundReturn, 1.0 / count) - 1;
            }

        }

        #endregion

        //......................................................................
        #region Calculated fields on SimResult
        //......................................................................
        public readonly record struct FailureBucket(int FromYear, int ToYear, int NumFailed);

        extension(SimResult simResult)
        {
            public int NoOfYears => simResult.Iterations.Count == 0 ? 0 : simResult.Iterations.Max(x => x.ByYear.Length);

            public double SurvivalRate => simResult.Iterations.Count == 0 ? 0.0 : (double)simResult.Iterations.Count(x => x.Success) / (double)simResult.Iterations.Count;

            public SimIteration Percentile(double percentile) =>
                percentile < 0.0 || percentile > 1.0 ? throw new ArgumentOutOfRangeException(nameof(percentile), "Percentile must be between 0.0 and 1.0") :
                simResult.Iterations.Count == 0 ? throw new InvalidOperationException("No results available") :
                0.0 == percentile ? simResult.Iterations.First() :
                1.0 == percentile ? simResult.Iterations.Last() :
                simResult.Iterations[(int)(percentile * (simResult.Iterations.Count - 1))];


            public IList<FailureBucket> GetFailureBuckets()
            {
                var numYears = simResult.NoOfYears;

                // Prepare five-year-buckets with start and end years.
                // You can't directly use GroupBy() on Model.Iterations, which will miss buckets with no failures.
                var fiveYearBuckets = Enumerable.Range(0, numYears).Select(y => y / 5).Distinct().Select(y => new { Start = 1 + (y * 5), End = 5 + (y * 5) });

                // Count no of failures in each bucket.
                // Note: Failed year = One Plus SurvivedYears
                return fiveYearBuckets
                    .Select(y => new FailureBucket
                    (
                        FromYear: y.Start,
                        ToYear: y.End,
                        NumFailed: simResult.Iterations.Count(x => !x.Success && x.SurvivedYears + 1 >= y.Start && x.SurvivedYears + 1 <= y.End)
                    ))
                    .ToList();
            }
        }

        #endregion

    }
}
