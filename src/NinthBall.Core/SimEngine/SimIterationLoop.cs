
namespace NinthBall.Core
{
    /// <summary>
    /// Represents single iteration of a simulation
    /// </summary>
    internal static class SimIterationLoop
    {
        public static SimIteration RunOneIteration(int iterationIndex, SimParams simParams, Initial initial, IReadOnlyList<ISimObjective> simObjectives, TaxAndMarketAssumptions TAMA, Memory<SimYear> iterationStore)
        {
            ArgumentNullException.ThrowIfNull(simParams);
            ArgumentNullException.ThrowIfNull(initial);
            ArgumentNullException.ThrowIfNull(simObjectives);

            int startAge = simParams.StartAge;
            int numYears = simParams.NoOfYears;
            Assets initialBalance = new
            (
                new(initial.PreTax.Amount, initial.PreTax.Allocation),
                new(initial.PostTax.Amount, initial.PostTax.Allocation),
                new(initial.YearZeroCashBalance, Allocation: 1.0)
            );

            // Strategies can be stateful. Each iteration creates a fresh set.
            var strategies = simObjectives.Select(x => x.CreateStrategy(iterationIndex)).ToList();

            // SimState represents the working memory of the iteration.
            var simState = new SimState(iterationIndex, startAge, initialBalance, iterationStore);

            // Assess each year. Exit if failed.
            bool survived = true;
            for(int yearIndex  = 0; yearIndex < numYears && survived; yearIndex++)
            {
                // Start fresh year.
                simState.BeginYear(yearIndex);

                // Apply all strategies
                foreach (var s in strategies) s.Apply(simState);

                // Before we start working on the suggestions...
                // Reject negative numbers, and drop double-precision-dust.
                simState.Fees        = simState.Fees.ThrowIfNegative().RoundToCents();
                simState.Taxes       = simState.Taxes.ThrowIfNegative().RoundToCents();
                simState.Incomes     = simState.Incomes.ThrowIfNegative().RoundToCents();
                simState.Expenses    = simState.Expenses.ThrowIfNegative().RoundToCents();
                simState.Withdrawals = simState.Withdrawals.ThrowIfNegative().RoundToCents();

                // Check for survivability, optimize and implement the strategy suggestions.
                (survived, var simYear) = simState.FinalizeYear(TAMA);

                // Capture the year to the history (even the failed year)
                simState.EndYear(simYear);
            }

            // Return iteration details.
            return new(iterationIndex, Success: survived, simState.PriorYears);
        }

        static (bool success, SimYear simYear) FinalizeYear(this IReadOnlySimState simState, TaxAndMarketAssumptions TAMA)
        {
            // Adjust withdrawals to meet the expenses.
            // See if we survived.
            var (success, adjustedWithdrawals, adjustedDeposits) = simState.FinalizeWithdrawals();

            if (success)
            {
                // Apply the changes, capture year-end balance
                Assets dec = simState.Jan
                    .ApplyFees(simState.Fees)
                    .Withdraw(adjustedWithdrawals)
                    .Grow(simState.ROI, out var changes, out var portfolioReturn)
                    .Deposit(adjustedDeposits)
                    ;

                // Update the inflation index first.
                // Growth calculation requires current year updated inflation index.
                RInflationIndex cyInflationIndex = simState.PriorYear.InflationIndex.Update
                (
                    cyInflationRate: simState.ROI.InflationRate,
                    federalTaxInflationLagHaircut: TAMA.FedTaxInflationLagHaircut,
                    stateTaxInflationLagHaircut: TAMA.StateTaxInflationLagHaircut
                );

                // Update the running annualized growth and real annualized growth.
                RGrowth cyGrowth = simState.PriorYear.RunningGrowth.Update
                (
                    simState.YearIndex,
                    portfolioReturn,
                    cyInflationIndex 
                );

                // Capture the year performance.
                SimYear successYear = new 
                (
                    simState.YearIndex,
                    simState.Age,
                    simState.Rebalanced,
                    simState.Jan,
                    simState.Fees,
                    simState.Taxes,
                    simState.Incomes,
                    simState.Expenses,
                    simState.ROI,

                    Withdrawals:    adjustedWithdrawals,
                    Deposits:       adjustedDeposits,
                    Change:         changes,
                    Dec:            dec,

                    RunningGrowth:  cyGrowth,
                    InflationIndex: cyInflationIndex
                );

                // Validate the calculation integrity, and return success result.
                return (success: true, successYear.ValidateMath());
            }
            else
            {
                SimYear failedYear = new
                (
                    simState.YearIndex,         // We know this
                    simState.Age,               // We know this
                    simState.Rebalanced,        // We know we did this
                    simState.Jan,               // We know the starting balances
                    simState.Fees,              // We know the fees since we know the starting balances
                    simState.Taxes,             // We know prior year taxes
                    simState.Incomes,           // We know the additional known incomes (if any) 
                    simState.Expenses,          // We know the estimated expenses that we could not meet

                    Withdrawals: default,       // Since we didn't withdraw any amount.
                    Deposits: default,          // Since we can't even meet expenses.
                    ROI: default,               // Irrelevant
                    Change: default,            // Since ROI is irrelevant
                    Dec: default,               // Would have spent any left-overs before Dec anyway
                    
                    RunningGrowth:  RGrowth.Invalid,         // Meaningless for failed year.
                    InflationIndex: RInflationIndex.Invalid  // Meaningless for failed year.
                );

                return (success: false, failedYear);
            }
        }

        static (bool success, Withdrawals adjustedWithdrawals, Deposits adjustedDeposits) FinalizeWithdrawals(this IReadOnlySimState context)
        {
            // Model works on all +ve numbers. 
            // We are responsible for correct application of sign.
            // Pre-validate our assumption.
            // BY-DESIGN: Invariants use Pascal case local variable names.
            var Jan = context.Jan.ThrowIfNegative();
            var Fees = context.Fees.ThrowIfNegative().RoundToCents();
            var Taxes = context.Taxes.ThrowIfNegative().RoundToCents();
            var Incomes = context.Incomes.ThrowIfNegative();
            var Expenses = context.Expenses.ThrowIfNegative();
            var SuggestedWithdrawals = context.Withdrawals.ThrowIfNegative();

            // Temp working memory, we will adjust these numbers.
            ThreeD available = new(Jan.PreTax.Amount, Jan.PostTax.Amount, Jan.Cash.Amount);
            ThreeD withdrawals = new(SuggestedWithdrawals.PreTax, SuggestedWithdrawals.PostTax, SuggestedWithdrawals.Cash);
            TwoD deposits = new(0, 0);

            // Fees goes first. 
            available.PreTax -= Fees.PreTax;
            available.PostTax -= Fees.PostTax;

            // Fees can't make balance go negative. If that happens, assume zero balance.
            available.PreTax = Math.Max(0.0, available.PreTax);
            available.PostTax = Math.Max(0.0, available.PostTax);
            available.Cash = Math.Max(0.0, available.Cash);

            // We can't withdraw more than what we have after deducting fees.
            withdrawals.PreTax = Math.Min(available.PreTax, withdrawals.PreTax);
            withdrawals.PostTax = Math.Min(available.PostTax, withdrawals.PostTax);
            withdrawals.Cash = Math.Min(available.Cash, withdrawals.Cash);

            // Take the withdrawals
            available.PreTax -= withdrawals.PreTax;
            available.PostTax -= withdrawals.PostTax;
            available.Cash -= withdrawals.Cash;

            // Do we have enough? 
            double deficit = Math.Max(0, Taxes.Total + Expenses.Total - Incomes.Total - withdrawals.Total);
            if (deficit.IsMoreThanZero(Precision.Amount))
            {
                // Model is not withdrawing enough.
                // If we need more, try take more from POST-Tax assets (Minimize tax, try to honor PreTax withdrawal velocity)
                TryTransferFunds(ref deficit, ref available.PostTax, ref withdrawals.PostTax);

                // If we need more, try take more from Pre-Tax assets (cash reserve is for emergency)
                TryTransferFunds(ref deficit, ref available.PreTax, ref withdrawals.PreTax);

                // If we need more, try cash-reserve (survival is important than future emergency)
                TryTransferFunds(ref deficit, ref available.Cash, ref withdrawals.Cash);

                // If we still need more, we didn't survive this year.
                if (deficit.IsMoreThanZero(Precision.Amount)) return (false, default, default);
            }

            // We survived.
            // We may even have some surplus.
            // All remaining surplus (if any) gets re-invested in post-tax assets
            var surplus = Math.Max(0, Incomes.Total + withdrawals.Total - Taxes.Total -  Expenses.Total);
            if (surplus.IsMoreThanZero(Precision.Amount))
            {
                deposits.PostTax += surplus;
                surplus = 0;
            }

            // We survived, and this is our final withdrawal and deposits (if any).
            var adjustedWithdrawals = new Withdrawals(withdrawals.PreTax, withdrawals.PostTax, withdrawals.Cash);
            var adjustedDeposits = new Deposits(deposits.PostTax, deposits.Cash);
            return (true, adjustedWithdrawals, adjustedDeposits);

            // Try to transfer suggested funds from source to target.
            static void TryTransferFunds(ref double need, ref double source, ref double target)
            {
                if (need.IsMoreThanZero(Precision.Amount) && source.IsMoreThanZero(Precision.Amount))
                {
                    double taking = Math.Min(need, source);
                    source -= taking;
                    target += taking;
                    need   -= taking;
                }
            }
        }

        static SimYear ValidateMath(this SimYear y)
        {
            y.Jan.ThrowIfNegative();
            y.Fees.ThrowIfNegative();
            y.Incomes.ThrowIfNegative();
            y.Expenses.ThrowIfNegative();
            y.Withdrawals.ThrowIfNegative();
            y.Deposits.ThrowIfNegative();
            y.Dec.ThrowIfNegative();

            var good = true;

            // Cashflow: (Incomes + Withdrawals) = (Taxes + Expenses + Deposits)
            good = (y.Incomes.Total + y.Withdrawals.Total).AlmostSame(y.Taxes.Total + y.Expenses.Total + y.Deposits.Total, Precision.Amount);
            if (!good) throw new Exception($"Inflow(Incomes + Withdrawals) doesn't match Outflow(Taxes + Expenses + Deposits)");

            // Expenses are met.
            good = (y.Incomes.Total + y.Withdrawals.Total + Precision.Amount) >= (y.Taxes.Total + y.Expenses.Total);
            if (!good) throw new Exception($"Inflow(Incomes + Withdrawals) is less than Outflow(Taxes + Expenses)");

            // Cash flow from starting to ending balances (and the in-between exchanges) agree.
            good &= (y.Jan.PreTax.Amount - y.Fees.PreTax - y.Withdrawals.PreTax + y.Change.PreTax).AlmostSame(y.Dec.PreTax.Amount, Precision.Amount);
            good &= (y.Jan.PostTax.Amount - y.Fees.PostTax - y.Withdrawals.PostTax + y.Change.PostTax + y.Deposits.PostTax).AlmostSame(y.Dec.PostTax.Amount, Precision.Amount);
            good &= (y.Jan.Cash.Amount - y.Withdrawals.Cash + y.Deposits.Cash).AlmostSame(y.Dec.Cash.Amount, Precision.Amount);
            if (!good) throw new Exception("(Jan - Fees - Withdrawals + Change + Deposits) doesn't match Dec balance.");

            // Either withdrawals or Deposits should be zero.
            good &= y.Withdrawals.PostTax.AlmostZero(Precision.Amount) || y.Deposits.PostTax.AlmostZero(Precision.Amount);
            good &= y.Withdrawals.Cash.AlmostZero(Precision.Amount) || y.Deposits.Cash.AlmostZero(Precision.Amount);
            if (!good) throw new Exception($"Meaningless fund transfers. Both withdrawal and deposit can't be positive.");

            return y;
        }

        //......................................................................
        #region ThrowIfNegative()
        //......................................................................
        static Assets ThrowIfNegative(this Assets x) { ThrowIfNegative(nameof(Assets), x.PreTax.Amount, x.PostTax.Amount, x.Cash.Amount); return x; }
        static Fees ThrowIfNegative(this Fees x) { ThrowIfNegative(nameof(Fees), x.PreTax, x.PostTax); return x; }
        static Taxes ThrowIfNegative(this Taxes x) { ThrowIfNegative(nameof(Taxes), x.Federal.Tax, x.State.Tax); return x; }
        static Incomes ThrowIfNegative(this Incomes x) { ThrowIfNegative(nameof(Incomes), x.SS, x.Ann); return x; }
        static Expenses ThrowIfNegative(this Expenses x) { ThrowIfNegative(nameof(Expenses), x.LivExp); return x; }
        static Withdrawals ThrowIfNegative(this Withdrawals x) { ThrowIfNegative(nameof(Withdrawals), x.PreTax, x.PostTax, x.Cash); return x; }
        static Deposits ThrowIfNegative(this Deposits x) { ThrowIfNegative(nameof(Deposits), x.PostTax, x.Cash); return x; }

        static bool ThrowIfNegative(string kind, params double[] doubles) => MinOrZero(doubles) < 0.0 ? throw new Exception($"{kind} cannot be negative.") : true;

        static double MinOrZero(params double[] values)
        {
            if (values is null or { Length: 0 }) return 0.0;

            double minValue = values[0];
            for (int i = 0; i < values.Length; i++) minValue = Math.Min(minValue, values[i]);
            return minValue;
        }

        #endregion

        //......................................................................
        #region Temp data structures for tracking and adjusting numbers
        //......................................................................
        private struct ThreeD(double PreTax, double PostTax, double Cash)
        {
            // Temp data structure to track the three asset values.
            public double PreTax = PreTax;
            public double PostTax = PostTax;
            public double Cash = Cash;
            public double Total => PreTax + PostTax + Cash;
        }

        private struct TwoD(double PostTax, double Cash)
        {
            // Temp data structure to track assets except PreTax
            public double PostTax = PostTax;
            public double Cash = Cash;
            public double Total => PostTax + Cash;
        }

        #endregion

    }

}
