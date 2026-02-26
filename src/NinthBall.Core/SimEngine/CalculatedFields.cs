namespace NinthBall.Core
{
    public static class CalculatedFields
    {
        extension(in Assets A)
        {
            // Total asset value.
            public double Total => A.PreTax.Amount + A.PostTax.Amount + A.Cash.Amount;

            // Approximate Tax-adjusted portfolio value (indicative value)
            // This is not cash in hand, if you try to withdraw all amount, will push you into a higher tax bracket.
            // BY-DESIGN: Ignoring cash basis, loss-harvesting (Taxes are overstated), all are qualified or long term gains (Taxes are understated).
            public double AfterTaxNetWorth => (A.PreTax.Amount * 0.75) + (A.PostTax.Amount * 0.85) + A.Cash.Amount;

        }

        extension(Rebalanced R)
        {
            public double StocksChange => R.PreTax.StocksChange + R.PostTax.StocksChange;     // Total change in stocks assets due to rebalancing
            public double BondsChange  => R.PreTax.BondsChange  + R.PostTax.BondsChange;      // Total change in stocks assets due to rebalancing
        }


        extension(Fees F)
        {
            public double Total => F.PreTax + F.PostTax;
        }

        extension (Incomes I)
        {
            public double Total => I.SS + I.Ann;
        }

        extension (Expenses E)
        {
            public double Total => E.LivExp;
        }

        extension (Withdrawals W)
        {
            public double Total => W.PreTax + W.PostTax + W.Cash;
        }

        extension (Deposits D)
        {
            public double Total => D.PostTax + D.Cash;
        }

        extension (Change C)
        {
            public double Total => C.PreTax + C.PostTax;
        }

        //......................................................................
        // Calculated fields on SimYear
        //......................................................................
        extension(in SimYear Y)
        {
            // Net change
            public double XPreTax   => 0 - Y.Withdrawals.PreTax;
            public double XPostTax  => Y.Deposits.PostTax - Y.Withdrawals.PostTax;
            public double XCash     => Y.Deposits.Cash - Y.Withdrawals.Cash;
            public double DecReal   => Y.Dec.Total / Math.Max(Y.InflationIndex.Consumer, Precision.Rate);
        }

        //......................................................................
        // Calculated fields on SimIteration 
        //......................................................................
        extension(SimIteration iteration)
        {
            public int SurvivedYears => iteration.Success ? iteration.ByYear.Length : Math.Max(0, iteration.ByYear.Length - 1);
            
            /// <summary>
            /// Last year of successful iteration, last-but-one year of failed iterations (Empty on zero survived years)
            /// </summary>
            public SimYear LastGoodYear => iteration.SurvivedYears > 0 ? iteration.ByYear.Span[iteration.SurvivedYears - 1] : new();

            /// <summary>
            /// Ending balance of last survived year, adjusted for inflation.
            /// </summary>
            public double EndingBalanceReal => iteration.LastGoodYear.Dec.Total / Math.Max(iteration.LastGoodYear.InflationIndex.Consumer, Precision.Rate);

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
        }

        //......................................................................
        // Calculated fields on SimResult
        //......................................................................
        public readonly record struct FailureBucket(int FromYear, int ToYear, int NumFailed);

        extension(SimResult simResult)
        {
            //public int NoOfYears => simResult.Input.SimParams.NoOfYears;

            public double SurvivalRate => simResult.Iterations.Count == 0 ? 0.0 : (double)simResult.Iterations.Count(x => x.Success) / (double)simResult.Iterations.Count;

            /// <summary>
            /// IMP: Iterations are expected to be pre-ordered worst-to-best based on chosen metrics.
            /// </summary>
            public SimIteration Percentile(double percentile) =>
                percentile < 0.0 || percentile > 1.0 ? throw new ArgumentOutOfRangeException(nameof(percentile), "Percentile must be between 0.0 and 1.0") :
                simResult.Iterations.Count == 0 ? throw new InvalidOperationException("No results available") :
                0.0 == percentile ? simResult.Iterations.First() :
                1.0 == percentile ? simResult.Iterations.Last() :
                simResult.Iterations[ (int)Math.Floor(percentile * (simResult.Iterations.Count - 1)) ];


            /// <summary>
            /// IMP: Iterations are expected to be pre-ordered worst-to-best based on chosen metrics.
            /// Reverse lookup of percentile-rank of the specific iteration.
            /// </summary>
            public double IterationPercentile(int iterationIndex)
            {
                // Find the position of this iteration in the ranked list
                // Calculate the percentile rank based on its relative position.
                for (int i = 0; i < simResult.Iterations.Count; i++)
                {
                    if (simResult.Iterations[i].Index == iterationIndex)
                        return (double)i / (simResult.Iterations.Count - 1);
                }
                return -1.0; // Not found
            }

            /// <summary>
            /// Returns number of failures by year-range.
            /// </summary>
            public IList<FailureBucket> GetFailureBuckets()
            {
                var numYears = simResult.SimParams.NoOfYears;

                // Prepare five-year-buckets with start and end years.
                // You can't directly use GroupBy() on Model.Iterations, which will miss buckets with no failures.
                var fiveYearBuckets = Enumerable.Range(0, (numYears + 4) / 5)
                    .Select(i => new
                    {
                        Start = 1 + i * 5,
                        End   = Math.Min(numYears, 5 + i * 5)
                    });

                // Count number of failures in each bucket.
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

    }
}
