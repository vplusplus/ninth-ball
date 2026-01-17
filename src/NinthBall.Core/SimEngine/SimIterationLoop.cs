
using DocumentFormat.OpenXml.Office2010.Drawing;

namespace NinthBall.Core
{
    /// <summary>
    /// Represents single iteration of a simulation
    /// </summary>
    internal static class SimIterationLoop
    {
        public static SimIteration RunOneIteration(int iterationIndex, SimParams simParams, Assets initialBalance, IReadOnlyList<ISimObjective> simObjectives, Memory<SimYear> iterationStore)
        {
            ArgumentNullException.ThrowIfNull(simParams);
            ArgumentNullException.ThrowIfNull(simObjectives);

            // Strategies can be stateful. Each iteration creates a fresh set.
            var strategies = simObjectives.Select(x => x.CreateStrategy(iterationIndex)).ToList();

            // SimState represents the working memory of the iteration.
            var simState = new SimState(iterationIndex, simParams.StartAge, initialBalance, iterationStore);

            // Asses each year. Exit if failed.
            bool success = false;
            for(int yearIndex  = 0; yearIndex < simParams.NoOfYears; yearIndex++)
            {
                simState.BeginYear(yearIndex);

                foreach (var s in strategies) s.Apply(simState);

                // Before we start working on the suggestions...
                // Reject negative numbers, and drop double-precision-dust.
                simState.Fees        = simState.Fees.ThrowIfNegative().RoundToCents();
                simState.Incomes     = simState.Incomes.ThrowIfNegative().RoundToCents();
                simState.Expenses    = simState.Expenses.ThrowIfNegative().RoundToCents();
                simState.Withdrawals = simState.Withdrawals.ThrowIfNegative().RoundToCents();

                (success, var simYear) = simState.FinalizeYear();
                simState.EndYear(simYear);

                if (!success) break;
            }

            return new(iterationIndex, success, simState.PriorYears);
        }


        private static (bool success, SimYear simYear) FinalizeYear(this IReadOnlySimState simState)
        {
            var (success, adjustedWithdrawals, adjustedDeposits) = simState.FinalizeWithdrawals();
            if (success)
            {
                Assets dec = simState.Jan
                    .ApplyFees(simState.Fees)
                    .Withdraw(adjustedWithdrawals)
                    .Grow(simState.ROI, out var changes, out var portfolioReturn)
                    .Deposit(adjustedDeposits)
                    ;

                Metrics metrics = simState.PriorYearMetrics.UpdateRunningMetrics(simState.YearIndex, portfolioReturn: portfolioReturn, inflationRate: simState.ROI.InflationRate);

                SimYear successYear = new 
                (
                    simState.YearIndex,
                    simState.Age,
                    simState.Jan,
                    simState.Fees,
                    simState.Incomes,
                    simState.Expenses,

                    Withdrawals: adjustedWithdrawals,
                    Deposits: adjustedDeposits,
                    ROI: simState.ROI,
                    Change: changes,
                    Dec: dec,
                    Metrics: metrics
                );

                return (success, successYear.ValidateMath());
            }
            else
            {
                SimYear failedYear = new
                (
                    simState.YearIndex,         // We know this
                    simState.Age,               // We know this
                    simState.Jan,               // We know the starting balances
                    simState.Fees,              // We know the fees since we know the starting balances
                    simState.Incomes,           // These are known incomes. 
                    simState.Expenses,          // We know the taxes-due and estimated expenses

                    Withdrawals: default,       // Since we didn't withdraw any amount.
                    Deposits: default,          // Since we can't even meet expenses.
                    ROI: default,               // Irrelevant
                    Change: default,            // Since ROI is irrelevant
                    Dec: default,               // User would have spend left-overs before Dec anyway

                    Metrics: new(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN)
                );

                return (success, failedYear);
            }
        }

        public static Metrics UpdateRunningMetrics(this Metrics pyMetrics, int yearIndex, double portfolioReturn, double inflationRate)
        {
            // Not trusting external year #0 initialization...
            Metrics prior = yearIndex > 0 ? pyMetrics : new
            (
                InflationMultiplier: 1.0,
                GrowthMultiplier: 1.0,
                PortfolioReturn: 0.0,
                AnnualizedReturn: 0.0,
                RealAnnualizedReturn: 0.0
            );

            // Running multiplier that represents cumulative inflation impact since year #0
            var inflationMultiplier = prior.InflationMultiplier * (1 + inflationRate);

            // Running multipler that represents cumulative growth since year #0
            double cumulativeGrowthMultiplier = prior.GrowthMultiplier * (1 + portfolioReturn);

            // Annualized nominal return since year #0
            double annualizedReturn = Math.Pow(cumulativeGrowthMultiplier, 1.0 / (yearIndex + 1)) - 1.0;

            // Real annualized return (adjusted for inflation) since year #0
            double realAnnualizedReturn = Math.Pow(cumulativeGrowthMultiplier / inflationMultiplier, 1.0 / (yearIndex + 1)) - 1.0;

            // Update the running metrics, return updated metrics.
            return new
            (
                InflationMultiplier: inflationMultiplier,
                GrowthMultiplier: cumulativeGrowthMultiplier,

                PortfolioReturn: portfolioReturn,
                AnnualizedReturn: annualizedReturn,
                RealAnnualizedReturn: realAnnualizedReturn
            );
        }


        //......................................................................
        #region FinalizeWithdrawals()
        //......................................................................
        static (bool success, Withdrawals adjustedWithdrawals, Deposits adjustedDeposits) FinalizeWithdrawals(this IReadOnlySimState context)
        {
            // Model works on all +ve numbers. 
            // We are responsible for correct application of sign.
            // Pre-validate our assumption.
            // BY-DESIGN: Invariants use Pascal case local variable names.
            var Jan = context.Jan.ThrowIfNegative();
            var Fees = context.Fees.ThrowIfNegative().RoundToCents();
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
            double deficit = Math.Max(0, Expenses.Total - Incomes.Total - withdrawals.Total);
            if (deficit.IsMoreThanZero(Precision.Amount))
            {
                // Model is not withdrawing enough.
                // If we need more, try take more from POST-Tax assets (Minimize tax, try to honor PreTax withdrawal velocity)
                TryTransferFunds(ref deficit, ref available.PostTax, ref withdrawals.PostTax);

                // If we need more, try take more from Pre-Tax assets (cash reserve is for emergency)
                TryTransferFunds(ref deficit, ref available.PreTax, ref withdrawals.PreTax);

                // If we need more, try cash-reserve (survival is imprtant than future emergency)
                TryTransferFunds(ref deficit, ref available.Cash, ref withdrawals.Cash);

                if (deficit.IsMoreThanZero(Precision.Amount)) return (false, default, default);
            }

            // We survived.
            // We may even have some surplus.
            // All remaining surplus (if any) gets re-invested in post-tax assets
            var surplus = Math.Max(0, Incomes.Total + withdrawals.Total - Expenses.Total);
            if (surplus.IsMoreThanZero(Precision.Amount))
            {
                deposits.PostTax += surplus;
                surplus = 0;
            }

            // This is our final withdrawal and deposit (if any) amounts and we survived.
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
                    need -= taking;
                }
            }
        }

        //......................................................................
        // Temp data structures for tracking and adjusting numbers
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

        //......................................................................
        #region RoundToCents() extensions
        //......................................................................

        static Fees RoundToCents(this Fees fees) => new
        (
            PreTax:  fees.PreTax.RoundToCents(),
            PostTax: fees.PostTax.RoundToCents()
        );

        static Incomes RoundToCents(this Incomes x) => new
        (
            SS: x.SS.RoundToCents(),
            Ann: x.Ann.RoundToCents()
        );

        static Expenses RoundToCents(this Expenses x) => new
        (
            PYTax:  x.PYTax.RoundToCents(),
            LivExp: x.LivExp.RoundToCents()
        );

        static Tax RoundToCents(this Tax x) => new
        (
            TaxOnOrdInc:  x.TaxOnOrdInc.RoundToCents(),
            TaxOnDiv:     x.TaxOnDiv.RoundToCents(),
            TaxOnInt:     x.TaxOnInt.RoundToCents(),
            TaxOnCapGain: x.TaxOnCapGain.RoundToCents()
        );

        static Withdrawals RoundToCents(this Withdrawals x) => new
        (
            PreTax:  x.PreTax.RoundToCents(),
            PostTax: x.PostTax.RoundToCents(),
            Cash:    x.Cash.RoundToCents()
        );

        static Deposits RoundToCents(this Deposits x) => new
        (
            PostTax: x.PostTax.RoundToCents(),
            Cash:    x.Cash.RoundToCents()
        );

        private static double RoundToCents(this double amount) => Math.Round(amount, 2);

        #endregion

        //......................................................................
        #region ThrowIfNegative()
        //......................................................................
        static Assets ThrowIfNegative(this Assets x) { ThrowIfNegative(nameof(Assets), x.PreTax.Amount, x.PostTax.Amount, x.Cash.Amount); return x; }
        static Fees ThrowIfNegative(this Fees x) { ThrowIfNegative(nameof(Fees), x.PreTax, x.PostTax); return x; }
        static Incomes ThrowIfNegative(this Incomes x) { ThrowIfNegative(nameof(Incomes), x.SS, x.Ann); return x; }
        static Expenses ThrowIfNegative(this Expenses x) { ThrowIfNegative(nameof(Expenses), x.PYTax.TaxOnOrdInc, x.PYTax.TaxOnDiv, x.PYTax.TaxOnInt, x.PYTax.TaxOnCapGain, x.LivExp); return x; }
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

        static SimYear ValidateMath(this SimYear y)
        {
            y.Jan.ThrowIfNegative();
            y.Dec.ThrowIfNegative();
            y.Fees.ThrowIfNegative();

            var good = true;

            // Cashflow: (Incomes + Withdrawals) = (Expenses + Deposits)
            good = (y.Incomes.Total + y.Withdrawals.Total).AlmostSame(y.Expenses.Total + y.Deposits.Total, Precision.Amount);
            if (!good) throw new Exception($"Incomes {y.Incomes.Total:C0} + Withdrawals {y.Withdrawals.Total:C0} != Expenses {y.Expenses.Total:C0} + Deposits {y.Deposits.Total:C0}");

            // Expenses are met.
            good = (y.Incomes.Total + y.Withdrawals.Total + Precision.Amount) >= y.Expenses.Total;
            if (!good) throw new Exception($"Incomes {y.Incomes.Total:C0} + Withdrawals {y.Withdrawals.Total:C0} is less than expenses {y.Expenses.Total:C0}");

            // Starting and ending balances agree: a.Starting - a.Fees - a.Withdrawals = a.Available
            good &= (y.Jan.PreTax.Amount - y.Fees.PreTax - y.Withdrawals.PreTax + y.Change.PreTax).AlmostSame(y.Dec.PreTax.Amount, Precision.Amount);
            good &= (y.Jan.PostTax.Amount - y.Fees.PostTax - y.Withdrawals.PostTax + y.Change.PostTax + y.Deposits.PostTax).AlmostSame(y.Dec.PostTax.Amount, Precision.Amount);
            good &= (y.Jan.Cash.Amount - y.Withdrawals.Cash + y.Deposits.Cash).AlmostSame(y.Dec.Cash.Amount, Precision.Amount);
            if (!good) throw new Exception("Balance reconciliation failed: Jan - Fees - Withdrawals + Change + Deposits != Dec");

            // Either withdrawals or Deposits should be zero.
            good &= y.Withdrawals.PostTax.AlmostZero(Precision.Amount) || y.Deposits.PostTax.AlmostZero(Precision.Amount);
            good &= y.Withdrawals.Cash.AlmostZero(Precision.Amount) || y.Deposits.Cash.AlmostZero(Precision.Amount);
            if (!good) throw new Exception($"Meaningless fund transfers. Both withdrawal and deposit can't be positive.");

            return y;
        }

    }

}
