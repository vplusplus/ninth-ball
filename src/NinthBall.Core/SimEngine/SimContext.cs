

namespace NinthBall.Core
{

    /// <summary>
    /// Tracks running balances.
    /// Provides a working-memory for strategy recommendations.
    /// Validates and applies the strategy recommendations.
    /// </summary>
    sealed class SimContext : ISimContext
    {
        // ..........................................
        // Running balances and prior year results
        // ..........................................
        private readonly SplitBalance MyPreTaxBalance  = new();
        private readonly SplitBalance MyPostTaxBalance = new();
        private readonly CashBalance  MyCashBalance    = new();
        private Memory<SimYear>       MyPriorYears;
        private double                MyRunningInflationMultiplier = 1.0;
        private double                MyCumulativeGrowth    = 1.0;

        // ..........................................
        // View of the running balances and prior year results
        // ..........................................
        public IBalance PreTaxBalance  => MyPreTaxBalance;
        public IBalance PostTaxBalance => MyPostTaxBalance;
        public IBalance CashBalance    => MyCashBalance;
        public ReadOnlyMemory<SimYear> PriorYears => MyPriorYears.Slice(0, YearsCompleted);
        public double RunningInflationMultiplier => MyRunningInflationMultiplier;

        // ..........................................
        // About current iteration
        // ..........................................
        public int StartAge { get; private set; }
        public int IterationIndex { get; private set; }
        public int YearsCompleted { get; private set; }

        // ..........................................
        // Current year information and strategy recommendations
        // ..........................................
        public int YearIndex { get; private set; }
        public int Age => StartAge + YearIndex;

        public Fees Fees { get; set; }
        public Incomes Incomes { get; set; }
        public Expenses Expenses { get; set; }
        public Withdrawals Withdrawals { get; set; }
        public ROI ROI { get; set; }
        // ..........................................

        /// <summary>
        /// SimContext instances are pooled and re-used.
        /// Erase the memory of prior iteration, and start a fresh iteration.
        /// </summary>
        public void Reset(InitialBalance initialBalance, int iterationIndex, int startAge, Memory<SimYear> store)
        {
            MyPreTaxBalance.Reset(initialBalance.PreTax.Amount, initialBalance.PreTax.Allocation);
            MyPostTaxBalance.Reset(initialBalance.PostTax.Amount, initialBalance.PostTax.Allocation);
            MyCashBalance.Reset(initialBalance.Cash.Amount);

            MyPriorYears = store;

            IterationIndex = iterationIndex;
            StartAge = startAge;
            YearIndex = 0;
            YearsCompleted = 0;

            Fees = default;
            Incomes = default;
            Expenses = default;
            Withdrawals = default;
            ROI = default;

            MyRunningInflationMultiplier = 1.0;
            MyCumulativeGrowth = 1.0;
        }

        /// <summary>
        /// Erase the memory of prior year, and start a new year.
        /// </summary>
        public void BeginNewYear(int yearIndex)
        {
            YearIndex = yearIndex;
            Fees = default;
            Incomes = default;
            Expenses = default;
            Withdrawals = default;
            ROI = default;
        }

        /// <summary>
        /// Validate and apply strategy recommendations.
        /// Returns false if the model failed.
        /// </summary>
        public bool ImplementStrategies()
        {
            // Take snapshot of jan 1st balance
            var jan = new Assets(
                AsReadOnly(MyPreTaxBalance), 
                AsReadOnly(MyPostTaxBalance), 
                AsReadOnly(MyCashBalance)
            );

            // Validate and adjust withdrawals and deposits
            var success = SimFinalization.FinalizeWithdrawals
            (
                jan, 
                Fees, Incomes, Expenses, Withdrawals, 
                out var adjustedWithdrawal, out var adjustedDeposits
            );

            // If we survived, apply the changes.
            if (success)
            {
                // Fees goes first
                MyPreTaxBalance.Post(   -Fees.PreTax);
                MyPostTaxBalance.Post(  -Fees.PostTax);
                MyCashBalance.Post(     -Fees.Cash);

                // Take withdrawals
                MyPreTaxBalance.Post(   -adjustedWithdrawal.PreTax);
                MyPostTaxBalance.Post(  -adjustedWithdrawal.PostTax);
                MyCashBalance.Post(     -adjustedWithdrawal.Cash);

                // Apply ROI 
                var growth = new Change
                (
                    MyPreTaxBalance.Grow(   ROI.StocksROI, ROI.BondsROI),
                    MyPostTaxBalance.Grow(  ROI.StocksROI, ROI.BondsROI),
                    MyCashBalance.Grow(     ROI.CashROI)
                );

                // Apply deposits
                MyPostTaxBalance.Post(  adjustedDeposits.PostTax);
                MyCashBalance.Post(     adjustedDeposits.Cash);

                // Take snapshot of ending balance
                var dec = new Assets(
                    AsReadOnly(MyPreTaxBalance), 
                    AsReadOnly(MyPostTaxBalance), 
                    AsReadOnly(MyCashBalance)
                );

                // ...........................................
                // Track inflation multiplier factor.
                // A number that represents inflation impact from year #0 to current year.
                // ...........................................
                MyRunningInflationMultiplier *= (1 + ROI.InflationRate);

                // ...........................................
                // Effective ROI after fees and withdrawals, but BEFORE year-end deposits.
                // BY-DESIGN: Effective ROI reflects only the invested assets (PreTax and PostTax).
                // Cash assets are excluded by design.
                // ...........................................
                double investedAssets = (jan.PreTax.Amount - Fees.PreTax - adjustedWithdrawal.PreTax) + (jan.PostTax.Amount - Fees.PostTax - adjustedWithdrawal.PostTax);
                double effectiveROI   = investedAssets > 1e-9 ? (growth.PreTax + growth.PostTax) / investedAssets : 0;

                // ...........................................
                // Track cumulative growth through this year.
                // Calculate RunningAnnualizedROI through this year.
                // ...........................................
                MyCumulativeGrowth *= (1 + effectiveROI);
                double runningAnnROI = Math.Pow(MyCumulativeGrowth, 1.0 / (YearIndex + 1)) - 1.0;
                double avgInflation  = Math.Pow(MyRunningInflationMultiplier, 1.0 / (YearIndex + 1)) - 1.0;
                double realAnnROI    = ((1 + runningAnnROI) / (1 + avgInflation)) - 1;

                // Track year-by-year performance.
                MyPriorYears.Span[YearIndex] = new SimYear
                (
                    YearIndex, Age, 
                    jan, 
                    Fees, Incomes, Expenses, 
                    adjustedWithdrawal, adjustedDeposits,
                    ROI, growth, 
                    dec,

                    EffectiveROI: effectiveROI,
                    RunningInflationMultiplier: MyRunningInflationMultiplier,
                    RunningAnnualizedROI: runningAnnROI,
                    RealAnnualizedROI: realAnnROI
                );
            }
            else
            {
                MyPriorYears.Span[YearIndex] = new SimYear
                (
                    //........................................
                    // We failed while planning on Jan 1st.
                    // Let's retain what we know till Jan 1st
                    //........................................
                    YearIndex,                  // We know this
                    Age,                        // We know this
                    jan,                        // We know the starting balances
                    Fees,                       // We know the fees since we know the starting balances
                    Incomes,                    // These are known incomes. 
                    Expenses,                   // We know the taxes-due and estimated expenses

                    //........................................
                    // We should not keep any data related to post Jan 1st.
                    // Even ROI and Inflation, we do not know on Jan 1st
                    // Keeping the incomplete ending balance is mis-leading.
                    //........................................
                    Withdrawals: default,       // Since we didn't withdraw any amount.
                    Deposits: default,          // Since we can't even meet expenses.
                    ROI: default,               // Irrelevant
                    Change: default,            // Since ROI is irrelevant
                    Dec: default,               // User would have spend left-overs before Dec anyway

                    //........................................
                    // Following values should never be used from failed iterations.
                    // If some parts of the solution (current or future) use this data, let them fail.
                    //........................................
                    EffectiveROI: double.NaN,  
                    RunningInflationMultiplier: double.NaN,
                    RunningAnnualizedROI: double.NaN,
                    RealAnnualizedROI: double.NaN 
                );
            }

            YearsCompleted++;
            return success;
        }

        private static Asset AsReadOnly(IBalance x) => new(x.Amount, x.Allocation);
    }
}
