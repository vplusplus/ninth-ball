

namespace NinthBall.Core
{
    internal static class SimFinalization
    {
        /// <summary>
        /// Given known constraints (Jan, fees, incomes, expenses, etc.)
        /// and given withdrawal and refill (deposit) aspirations,
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
            Deposits SuggestedRefills,              // Refill aspirations suggested by the model (we may adjust this)
            out Withdrawals adjustedWithdrawals,    // Adjusted withdrawal amounts that aligns with realities
            out Deposits adjustedDeposits           // Max permissible adjusted refill (deposit) amounts
        )
        {
            // Model works on all +ve numbers. 
            // We are responsible for correct application of sign.
            // Pre-validate our assumption.
            Jan.ThrowIfNegative();
            Fees.ThrowIfNegative();
            Incomes.ThrowIfNegative();
            Expenses.ThrowIfNegative();
            SuggestedRefills.ThrowIfNegative();
            SuggestedWithdrawals.ThrowIfNegative();

            // Temp working memory, we will adjust these numbers.
            ThreeD available   = new("Available", Jan.PreTax.Amount, Jan.PostTax.Amount, Jan.Cash.Amount);
            ThreeD withdrawals = new("Withdrawals", SuggestedWithdrawals.PreTax, SuggestedWithdrawals.PostTax, SuggestedWithdrawals.Cash);
            TwoD deposits      = new("Deposits", 0, 0);

            // Let's take care of one meaningless fund transfer.
            // Take 100K from post-tax, refill 50K to post-tax = take 50K from post-tax
            // Take 50K from cash, refill 100K to cash-buffer  = take Zero from cash. 
            withdrawals.PostTax = Math.Max(0, withdrawals.PostTax - SuggestedRefills.PostTax);
            withdrawals.Cash    = Math.Max(0, withdrawals.Cash - SuggestedRefills.Cash);

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

            // We will come to refill aspirations later.
            // First we will meet the expenses.
            // Do we have enough? 
            double deficit = Math.Max(0, Expenses.Total() - Incomes.Total() - withdrawals.Total());
            if (deficit.IsMoreThanZero(Precision.Amount))
            {
                // Model is not withdrawing enough; we need more.
                // Priority:
                // 1. PostTax - Minimize tax, try to honor PreTax withdrawal velocity
                // 2. PreTax  - Cash reserve is for emergency, give max control to the model
                // 3. Cash    - Survival is better than keeping the cash for future emergency
                TryTransferFunds(ref deficit, ref available.PostTax, ref withdrawals.PostTax);
                TryTransferFunds(ref deficit, ref available.PreTax,  ref withdrawals.PreTax);
                TryTransferFunds(ref deficit, ref available.Cash,    ref withdrawals.Cash);

                if (deficit.IsMoreThanZero(Precision.Amount))
                {
                    // Insufficient funds. Model failed.
                    adjustedWithdrawals = new();
                    adjustedDeposits = new();
                    return false;
                }
            }

            // We survived. We may even have some surplus.
            var surplus = Math.Max(0, Incomes.Total() + withdrawals.Total() - Expenses.Total());

            // First, we will try to accommodate cash-refill aspiration.
            var refillTarget = SuggestedRefills.Cash;
            if (refillTarget.IsMoreThanZero(Precision.Amount))
            {
                // Use unallocated cash-in-hand first
                // If we need more try PostTax: Minimize tax, try to honor PreTax withdrawal velocity
                // If we need more try PreTax:  Model is trying to catch-up with prior cash drain.
                TryTransferFunds(ref refillTarget, ref surplus, ref deposits.Cash);
                withdrawals.PostTax += TryTransferFunds(ref refillTarget, ref available.PostTax, ref deposits.Cash);
                withdrawals.PreTax  += TryTransferFunds(ref refillTarget, ref available.PreTax,  ref deposits.Cash);
            }

            // Now we try to accommodate fund transfer aspirations to PostTax account.
            refillTarget = SuggestedRefills.PostTax;
            if (refillTarget.IsMoreThanZero(Precision.Amount))
            {
                // Use unallocated cash-in-hand first
                // If we need more try PreTax; Model is trying to spread the tax impact of PreTax funds
                TryTransferFunds(ref refillTarget, ref surplus, ref deposits.PostTax);
                withdrawals.PreTax += TryTransferFunds(ref refillTarget, ref available.PreTax, ref deposits.PostTax);
            }

            // One more thing...
            // All remaining surplus (if any) gets re-invested in post-tax assets
            if (surplus.IsMoreThanZero(Precision.Amount))
            {
                deposits.PostTax += surplus;
                surplus = 0;
            }

            adjustedWithdrawals = new(withdrawals.PreTax, withdrawals.PostTax, withdrawals.Cash);
            adjustedDeposits    = new(deposits.PostTax, deposits.Cash);

            // Independent validation of math integrity
            VerifyWithdrawals(Jan, Fees, Incomes, Expenses, adjustedWithdrawals, adjustedDeposits, available);

            return true;

            // Try to transfer suggested funds from source to target.
            // If suggested amount is not whatIHave, transfer available funds.
            static double TryTransferFunds(ref double whatWeNeed, ref double source, ref double target)
            {
                if (whatWeNeed.IsMoreThanZero(Precision.Amount) && source.IsMoreThanZero(Precision.Amount))
                {
                    double took = Math.Min(whatWeNeed, source);
                    source -= took;
                    target += took;
                    whatWeNeed -= took;

                    return took;
                }
                else return 0;
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
        static void ThrowIfNegative(this Expenses x) => ThrowIfNegative(nameof(Expenses), x.PYTax, x.CYExp);
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
        private struct ThreeD(string name, double PreTax, double PostTax, double Cash)
        {
            // Temp data structure to track the three asset values.
            public double PreTax = PreTax;
            public double PostTax = PostTax;
            public double Cash = Cash;
            public double Total() => PreTax + PostTax + Cash;

            public override string ToString() => $"{name}: {{ PreTax: {PreTax:F0}, PostTax: {PostTax:F0}, Cash: {Cash:F0} }}";
        }

        private struct TwoD(string name, double PostTax, double Cash)
        {
            // Temp data structure to track assets except PreTax
            public double PostTax = PostTax;
            public double Cash = Cash;
            public double Total() => PostTax + Cash;

            public override string ToString() => $"{name}: {{ PostTax: {PostTax:F0}, Cash: {Cash:F0} }}";
        }

        #endregion

    }
}
