

namespace NinthBall.Core
{
    internal static class SimFinalization
    {
        /// <summary>
        /// Given known constraints (Jan, fees, incomes, expenses, etc.)
        /// calculates the financially viable withdrawal and deposit amounts.
        /// Returns false if essential expenses can't be met.
        /// </summary>
        public static bool FinalizeWithdrawals
        (
            Assets Jan,                             // Starting portfolio value as on Jan 1st
            Fees Fees,                              // Fee amounts payable on Jan 1st balance
            Incomes Incomes,                        // Known guaranteed additional incomes
            Expenses Expenses,                      // Prior year tax and current year expenses
            Withdrawals SuggestedWithdrawals,       // Withdrawal scheme suggested by the model (we may adjust this)
            out Withdrawals adjustedWithdrawals,    // Adjusted withdrawal amounts that aligns with realities
            out Deposits adjustedDeposits           // Deposit of excess income (if any)
        )
        {
            // Model works on all +ve numbers. 
            // We are responsible for correct application of sign.
            // Pre-validate our assumption.
            Jan.ThrowIfNegative();
            Fees.ThrowIfNegative();
            Incomes.ThrowIfNegative();
            Expenses.ThrowIfNegative();
            SuggestedWithdrawals.ThrowIfNegative();

            // Temp working memory, we will adjust these numbers.
            ThreeD available    = new(Jan.PreTax.Amount, Jan.PostTax.Amount, Jan.Cash.Amount);
            ThreeD withdrawals  = new(SuggestedWithdrawals.PreTax, SuggestedWithdrawals.PostTax, SuggestedWithdrawals.Cash);
            TwoD deposits       = new(0, 0);

            // Fees goes first. 
            available.PreTax   -= Fees.PreTax;
            available.PostTax  -= Fees.PostTax;
            available.Cash     -= Fees.Cash;

            // Fees can't make balance go negative. If that happens, assume zero balance.
            available.PreTax    = Math.Max(0.0, available.PreTax);
            available.PostTax   = Math.Max(0.0, available.PostTax);
            available.Cash      = Math.Max(0.0, available.Cash);

            // We can't withdraw more than what we have after deducting fees.
            withdrawals.PreTax  = Math.Min(available.PreTax,  withdrawals.PreTax);
            withdrawals.PostTax = Math.Min(available.PostTax, withdrawals.PostTax);
            withdrawals.Cash    = Math.Min(available.Cash,    withdrawals.Cash);

            // Take the withdrawals
            available.PreTax   -= withdrawals.PreTax;
            available.PostTax  -= withdrawals.PostTax;
            available.Cash     -= withdrawals.Cash;

            // Do we have enough? 
            double deficit = Math.Max(0, Expenses.Total() - Incomes.Total() - withdrawals.Total());
            if (deficit.IsMoreThanZero(Precision.Amount))
            {
                // Model is not withdrawing enough.
                // If we need more, try take more from POST-Tax assets (Minimize tax, try to honor PreTax withdrawal velocity)
                TryTransferFunds(ref deficit, ref available.PostTax, ref withdrawals.PostTax);

                // If we need more, try take more from Pre-Tax assets (cash reserve is for emergency)
                TryTransferFunds(ref deficit, ref available.PreTax,  ref withdrawals.PreTax);

                // If we need more, try cash-reserve (survival is imprtant than future emergency)
                TryTransferFunds(ref deficit, ref available.Cash, ref withdrawals.Cash);

                if (deficit.IsMoreThanZero(Precision.Amount))
                {
                    // Insufficient funds. Model failed.
                    adjustedWithdrawals = new();
                    adjustedDeposits = new();
                    return false;
                }
            }

            // We survived.
            // We may even have some surplus.
            // All remaining surplus (if any) gets re-invested in post-tax assets
            var surplus = Math.Max(0, Incomes.Total() + withdrawals.Total() - Expenses.Total());
            if (surplus.IsMoreThanZero(Precision.Amount))
            {
                deposits.PostTax += surplus;
                surplus = 0;
            }

            // This is our final withdrawal and deposit (if any) amounts.
            adjustedWithdrawals = new(withdrawals.PreTax, withdrawals.PostTax, withdrawals.Cash);
            adjustedDeposits    = new(deposits.PostTax, deposits.Cash);

            // Independent validation of math integrity
            VerifyWithdrawals(Jan, Fees, Incomes, Expenses, adjustedWithdrawals, adjustedDeposits, available);

            // Done.
            return true;

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

        /// <summary>
        /// Independent validation of the math.
        /// Focus on integrity of the end-result.
        /// Pretend you don't know anything about the calculations.
        /// Repeating all calculations is not the intention.
        /// </summary>
        static void VerifyWithdrawals(Assets Jan, Fees Fees, Incomes Incomes, Expenses Expenses, Withdrawals Withdrawals, Deposits Deposits, ThreeD Available)
        {
            Jan.ThrowIfNegative();
            Fees.ThrowIfNegative();
            Incomes.ThrowIfNegative();
            Expenses.ThrowIfNegative();
            Withdrawals.ThrowIfNegative();
            Deposits.ThrowIfNegative();
            Available.ThrowIfNegative();

            var good = true;

            // Cashflow: (Incomes + Withdrawals) = (Expenses + Deposits)
            good = (Incomes.Total() + Withdrawals.Total()).AlmostSame(Expenses.Total() + Deposits.Total(), Precision.Amount);
            if (!good) throw new Exception($"Incomes {Incomes.Total():C0} + Withdrawals {Withdrawals.Total():C0} != Expenses {Expenses.Total():C0} + Deposits {Deposits.Total():C0}");

            // Expenses are met.
            good = (Incomes.Total() + Withdrawals.Total() + Precision.Amount) >= Expenses.Total();
            if (!good) throw new Exception($"Incomes {Incomes.Total():C0} + Withdrawals {Withdrawals.Total():C0} is less than expenses {Expenses.Total():C0}");

            // Starting and ending balances agree: a.Starting - a.Fees - a.Withdrawals = a.Available
            good &= (Jan.PreTax.Amount  - Fees.PreTax  - Withdrawals.PreTax ).AlmostSame(Available.PreTax,  Precision.Amount);
            good &= (Jan.PostTax.Amount - Fees.PostTax - Withdrawals.PostTax).AlmostSame(Available.PostTax, Precision.Amount);
            good &= (Jan.Cash.Amount    - Fees.Cash    - Withdrawals.Cash   ).AlmostSame(Available.Cash,    Precision.Amount);
            if (!good) throw new Exception("Starting - Fees - Withdrawals != Available");

            // Either withdrawals or Deposits should be zero.
            good &= Withdrawals.PostTax.AlmostZero(Precision.Amount) || Deposits.PostTax.AlmostZero(Precision.Amount);
            good &= Withdrawals.Cash.AlmostZero(Precision.Amount)    || Deposits.Cash.AlmostZero(Precision.Amount);
            if (!good) throw new Exception($"Meaningless fund transfers. Both withdrawal and deposit can't be positive.");
        }

        //......................................................................
        #region Validation helpers 
        //......................................................................
        static void ThrowIfNegative(this Assets x) => ThrowIfNegative(nameof(Assets), x.PreTax.Amount, x.PostTax.Amount, x.Cash.Amount);
        static void ThrowIfNegative(this Fees x) => ThrowIfNegative(nameof(Fees), x.PreTax, x.PostTax, x.Cash);
        static void ThrowIfNegative(this Incomes x) => ThrowIfNegative(nameof(Incomes), x.SS, x.Ann);
        static void ThrowIfNegative(this Expenses x) => ThrowIfNegative(nameof(Expenses), x.PYTax.TaxOnOrdInc, x.PYTax.TaxOnDiv, x.PYTax.TaxOnInt, x.PYTax.TaxOnCapGain, x.LivExp);
        static void ThrowIfNegative(this Deposits x) => ThrowIfNegative(nameof(Deposits), x.PostTax, x.Cash);
        static void ThrowIfNegative(this Withdrawals x) => ThrowIfNegative(nameof(Withdrawals), x.PreTax, x.PostTax, x.Cash);
        static void ThrowIfNegative(this ThreeD x) => ThrowIfNegative(nameof(Withdrawals), x.PreTax, x.PostTax, x.Cash);
        static bool ThrowIfNegative(string kind, params double[] doubles) => Min(doubles) < 0 ? throw new Exception($"{kind} cannot be negative.") : true;

        static double Min(params double[] values)
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
            public double Total() => PreTax + PostTax + Cash;
        }

        private struct TwoD(double PostTax, double Cash)
        {
            // Temp data structure to track assets except PreTax
            public double PostTax = PostTax;
            public double Cash = Cash;
            public double Total() => PostTax + Cash;
        }

        #endregion

    }
}
