
namespace NinthBall.Core
{
    public static class CalculatedFields
    {
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
                    var jan = simYear.Jan;
                    var s1 = jan.PreTax.Amount + simYear.Fees.PreTax - simYear.Withdrawals.PreTax;
                    var s2 = jan.PostTax.Amount + simYear.Fees.PostTax- simYear.Withdrawals.PostTax;

                    // Stock and bond balances less fees and withdrawals
                    var S1 = s1 * jan.PreTax.StockAlloc;
                    var B1 = s1 * jan.PreTax.BondAlloc;
                    var S2 = s2 * jan.PostTax.StockAlloc;
                    var B2 = s2 * jan.PostTax.BondAlloc;

                    // Deposits are withheld from ROI calculation
                    // Simulation updates deposits after calculating ROI for the year

                    // Growth
                    var rS = simYear.ROI.StocksROI;
                    var rB = simYear.ROI.BondsROI;

                    // Change and PCT change.
                    var change = (S1 * rS) + (B1 * rB) + (S2 * rS) + (B2 * rB);
                    var pctChange = change / (s1 + s2);

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
            public double StartingBalance => iteration.ByYear.Span[0].Jan.Total();
            public double EndingBalance   => iteration.ByYear.Span[^1].Dec.Total();
            public int    SurvivedYears   => iteration.Success ? iteration.ByYear.Length : iteration.ByYear.Length - 1;
            public int    SurvivedAge     => iteration.Success ? iteration.ByYear.Span[^1].Age : iteration.ByYear.Span[^2].Age;

            // Aggregate totals
            public double SumFees         => iteration.Sum(x => x.Fees.Total());
            public double SumTaxes        => iteration.Sum(x => x.Expenses.PYTax.Total());
            public double SumExpenses     => iteration.Sum(x => x.Expenses.CYExp);
            public double SumSS           => iteration.Sum(x => x.Incomes.SS);
            public double SumAnn          => iteration.Sum(x => x.Incomes.Ann);
            public double SumPreTaxWDraw  => iteration.Sum(x => x.Withdrawals.PreTax);
            public double SumPostTaxWDraw => iteration.Sum(x => x.Withdrawals.PostTax);
            public double SumCashWDraw    => iteration.Sum(x => x.Withdrawals.Cash);
            public double SumChange       => iteration.Sum(x => x.Change.Total());


            /// <summary>
            /// Zero-copy extension to calculate the maximum maxValue of a selected field across all years in the iteration
            /// </summary>
            public double Max(Func<SimYear, double> fxValueSelector)
            {
                // TODO: Research use of function pointers for performance improvement
                // public unsafe double Max(delegate*<in SimYear, double> selector) { ... }

                ArgumentNullException.ThrowIfNull(fxValueSelector);

                var span = iteration.ByYear.Span;
                double maxValue = double.NegativeInfinity;
                for (int i = 0; i < span.Length; i++) maxValue = Math.Max(maxValue, fxValueSelector(span[i]));
                return maxValue;
            }

            /// <summary>
            /// Zero-copy extension to calculate the sum of a selected field across all years in the iteration 
            /// </summary>
            public double Sum(Func<SimYear, double> fxValueSelector)
            {
                ArgumentNullException.ThrowIfNull(fxValueSelector);

                var span = iteration.ByYear.Span;
                double sumValue = 0.0;
                for (int i = 0; i < span.Length; i++) sumValue += fxValueSelector(span[i]);
                return sumValue;
            }
        }

        #endregion

    }
}
