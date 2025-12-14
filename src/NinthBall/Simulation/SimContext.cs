

namespace NinthBall
{
    sealed record CashBalance : IBalance
    {
        double Cash = 0.0;

        public double Amount => Cash;
        public double Allocation => 1.0;

        // Single asset. Ignore allocation and drift.
        public void Init(double amount, double _) => Cash = amount;

        // Single asset. Nothing to Rebalance.
        public bool RebalanceIf(double _) => false;

        // Single asset. Nothing to Rellocate.
        public bool Reallocate(double _, double __) => false;

        public void Post(double amount) => Cash += amount;

        public double Grow(double ROI)
        {
            var change = Amount * ROI;
            Cash += change;
            return change;
        }
    }

    sealed record SplitBalance : IBalance
    {
        double Stocks = 0.0;
        double Bonds = 0.0;
        double TargetAllocation = 1.0;

        double CurrentAllocation => 0 == (Stocks + Bonds) ? 0.0 : Stocks / (Stocks + Bonds + 0.0001);
        double CurrentDrift => Math.Abs(CurrentAllocation - TargetAllocation) * 2;

        public double Amount => Stocks + Bonds;
        public double Allocation => CurrentAllocation;

        public void Init(double amount, double allocation)
        {
            Stocks = amount * allocation;
            Bonds = amount - Stocks;
            TargetAllocation = allocation;
        }

        public bool RebalanceIf(double maxDrift)
        {
            if (Math.Abs(CurrentDrift) > Math.Abs(maxDrift))
            {
                double tmpAmount = Stocks + Bonds;
                Stocks = tmpAmount * TargetAllocation;
                Bonds = tmpAmount - Stocks;
                return true;
            }
            else return false;
        }

        public bool Reallocate(double newAllocation, double maxDrift)
        {
            TargetAllocation = newAllocation;
            return RebalanceIf(maxDrift);
        }

        public void Post(double amount)
        {
            if (amount > 0.0) Deposit(amount); else if (amount < 0.0) Withdraw(Math.Abs(amount));

            void Deposit(double depositAmount)
            {
                if (0 == depositAmount) return;
                if (depositAmount < 0) throw new ArgumentException("Deposit amount must be positive.");

                var stockChange = depositAmount * TargetAllocation;
                var bondChange = depositAmount - stockChange;

                Stocks += stockChange;
                Bonds += bondChange;
            }

            void Withdraw(double withdrawalAmount)
            {
                if (0 == withdrawalAmount) return;
                if (withdrawalAmount < 0) throw new ArgumentException("Withdrawal amount must be positive.");
                if (withdrawalAmount > Amount + 0.01) throw new InvalidOperationException($"Can't withdraw more than what we have | Aailable: {Amount:C0} | Requested: {withdrawalAmount:C0}");

                // TODO: Revisit rounding issues Avoid roundng errors
                withdrawalAmount = Math.Min(withdrawalAmount, Amount);

                // This is how much we plan to reduce from stock and bond balance.
                double fromStock = withdrawalAmount * TargetAllocation;
                double fromBond = withdrawalAmount - fromStock;

                // Try to took from correct asset.
                TryTakeFrom(ref Stocks, ref fromStock);
                TryTakeFrom(ref Bonds, ref fromBond);

                // There may be insufficient funds. Try otherway.
                TryTakeFrom(ref Bonds, ref fromStock);
                TryTakeFrom(ref Stocks, ref fromBond);

                // We had enough funds. By now, both must be zero.
                if (fromStock + fromBond > 0.0001) throw new InvalidOperationException("Balance.Withdraw() - Unexpected mismatch in calculation.");
                return;

                static void TryTakeFrom(ref double availabe, ref double required)
                {
                    var took = Math.Min(availabe, required);
                    if (took > 0)
                    {
                        availabe -= took;
                        required -= took;
                    }
                }
            }
        }

        public double Grow(double stocksROI, double bondsROI)
        {
            var stockChange = Stocks * stocksROI;
            var bondsChange = Bonds * bondsROI;

            Stocks += stockChange;
            Bonds += bondsChange;

            return stockChange + bondsChange;
        }
    }

    sealed record SimContext(int IterationIndex) : ISimContext
    {
        private readonly SplitBalance MyPreTaxBalance = new();
        private readonly SplitBalance MyPostTaxBalance = new();
        private readonly CashBalance MyCashBalance = new();
        private readonly List<SimYear> MyPriorYears = new();

        // The zero based index of simulation year
        //public int IterationIndex { get; } = iterationIndex;
        public int YearIndex { get; private set; }

        // Running balance
        public IReadOnlyList<SimYear> PriorYears => MyPriorYears;
        public IBalance PreTaxBalance => MyPreTaxBalance;
        public IBalance PostTaxBalance => MyPostTaxBalance;
        public IBalance CashBalance => MyCashBalance;

        // Current year 
        public FeesPCT FeesPCT { get; set; }
        public Incomes Incomes { get; set; }
        public Expenses Expenses { get; set; }
        public Withdrawals Withdrawals { get; set; }
        public Deposits Refills { get; set; }
        public ROI ROI { get; set; }

        public void StartYear(int yearIndex)
        {
            FeesPCT = default;
            Incomes = default;
            Expenses = default;
            Withdrawals = default;
            Refills = default;
            ROI = default;
            YearIndex = yearIndex;
        }

        static Asset AsReadOnly(IBalance x) => new(x.Amount, x.Allocation);

        public bool EndYear()
        {
            var jan = new Assets(AsReadOnly(MyPreTaxBalance), AsReadOnly(MyPostTaxBalance), AsReadOnly(MyCashBalance));
            var fees = FeesPCT.CalculateFees(jan);

            var success = SimMath.FinalizeWithdrawals(
                jan,
                fees,
                Incomes, Expenses,
                Withdrawals, Refills,
                out var adjustedWithdrawals,
                out var adjustedDeposits
            );

            if (success)
            {
                //................................................
                // Apply the chages to the portfolio
                //................................................
                // Jan 1st - Apply the fees
                MyPreTaxBalance.Post(-fees.PreTax);
                MyPostTaxBalance.Post(-fees.PostTax);
                MyCashBalance.Post(-fees.Cash);

                // Jan 1st - Take withdrawals
                MyPreTaxBalance.Post(-adjustedWithdrawals.PreTax);
                MyPostTaxBalance.Post(-adjustedWithdrawals.PostTax);
                MyCashBalance.Post(-adjustedWithdrawals.Cash);

                // Some time after Jan 1st - Apply ROI
                var growth = new Change(
                    PreTax: MyPreTaxBalance.Grow(ROI.StocksROI, ROI.BondsROI),
                    PostTax: MyPostTaxBalance.Grow(ROI.StocksROI, ROI.BondsROI),
                    Cash: MyCashBalance.Grow(ROI.CashROI)
                );

                // Dec 31st - Apply the refills/fund transfers
                // REVISIT: Why deposits on Dec 31st if fund are idling through the year?
                // REVISIT: May be we should move this to Jan 1st as well.
                MyPostTaxBalance.Post(adjustedDeposits.PostTax);
                MyCashBalance.Post(adjustedDeposits.Cash);
                //................................................

                // Ending balance
                var dec = new Assets(AsReadOnly(MyPreTaxBalance), AsReadOnly(MyPostTaxBalance), AsReadOnly(MyCashBalance));

                MyPriorYears.Add(new SimYear(
                    YearIndex, jan, fees, Incomes, Expenses, adjustedWithdrawals, adjustedDeposits, ROI, growth, dec
                ));
            }
            else
            {
                MyPriorYears.Add(new SimYear()
                {
                    Year = YearIndex,
                    Jan = jan,
                });
            }

            return success;
        }

    }

    public static class SimMath
    {
        /// <summary>
        /// Given known constraints (Jan, fees, incomes, expenses, etc.)
        /// and given withdrawal and refill (deposit) aspirations,
        /// calculates the financially viable withdrawal and deposit amounts.
        /// Returns false if essential expenses can't be met.
        /// </summary>
        public static bool FinalizeWithdrawals
        (
            // Immutables uses camel-case variable names on purpose.
            Assets Jan,                          // Starting portfolio value as on Jan 1st
            Fees Fees,                              // Fee amounts payable on Jan 1st balance
            Incomes Incomes,                        // Known guarenteed additional incomes
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
            ThreeD available = new("Available", Jan.PreTax.Amount, Jan.PostTax.Amount, Jan.Cash.Amount);
            ThreeD withdrawals = new("Withdrawals", SuggestedWithdrawals.PreTax, SuggestedWithdrawals.PostTax, SuggestedWithdrawals.Cash);
            TwoD deposits = new("Deposits", 0, 0);

            // Let's took care of one meaningless fund transfer.
            // Take 100K from post-tax, refill 50K to post-tax = took 50K from post-tax
            // Take 50K from cash, refill 100K to cash-buffer  = took Zero from cash. 
            withdrawals.PostTax = Math.Max(0, withdrawals.PostTax - SuggestedRefills.PostTax);
            withdrawals.Cash = Math.Max(0, withdrawals.Cash - SuggestedRefills.Cash);

            // Fees goes first. 
            available.PreTax -= Fees.PreTax;
            available.PostTax -= Fees.PostTax;
            available.Cash -= Fees.Cash;

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

            // We will come to refill aspirations later.
            // First we will meet the expenses.
            // Do we have enough? 
            double deficit = Math.Max(0, Expenses.Total() - Incomes.Total() - withdrawals.Total());
            if (deficit.IsMoreThanZero())
            {
                // Model is not withdrawing enough; we need more.
                // Sequence:
                // 1. PostTax - Minimize tax, try to honor PreTax withdrawal velocity
                // 2. PreTax  - Cash reserve is for emergency, give max control to the model
                // 3. Cash    - Survical is better than keeping the cash for future emergency
                TryTransferFunds(ref deficit, ref available.PostTax, ref withdrawals.PostTax);
                TryTransferFunds(ref deficit, ref available.PreTax, ref withdrawals.PreTax);
                TryTransferFunds(ref deficit, ref available.Cash, ref withdrawals.Cash);

                if (deficit.IsMoreThanZero())
                {
                    // Insufficient funds. Model failed.
                    adjustedWithdrawals = new();
                    adjustedDeposits = new();
                    return false;
                }
            }

            // We survived. We may even have some surplus.
            var surplus = Math.Max(0, Incomes.Total() + withdrawals.Total() - Expenses.Total());

            // First, we will try to accomodate cash-refill aspiration.
            var refillTarget = SuggestedRefills.Cash;
            if (refillTarget.IsMoreThanZero())
            {
                // Use unallocated cash-in-hand first
                // If we need more try PostTax: Minimize tax, try to honor PreTax withdrawal velocity
                // If we need more try PreTax:  Model is trying to catch-up with prior cash drain.
                TryTransferFunds(ref refillTarget, ref surplus, ref deposits.Cash);
                withdrawals.PostTax += TryTransferFunds(ref refillTarget, ref available.PostTax, ref deposits.Cash);
                withdrawals.PreTax += TryTransferFunds(ref refillTarget, ref available.PreTax, ref deposits.Cash);
            }

            // Now we try to accomodate fund transfer aspirations to PostTax account.
            refillTarget = SuggestedRefills.PostTax;
            if (refillTarget.IsMoreThanZero())
            {
                // Use unallocated cash-in-hand first
                // If we need more try PreTax:  Model is trying to spread the tax impact of PreTax funds
                TryTransferFunds(ref refillTarget, ref surplus, ref deposits.PostTax);
                withdrawals.PreTax += TryTransferFunds(ref refillTarget, ref available.PreTax, ref deposits.PostTax);
            }

            // One more thing...
            // All remaining surplus (if any) gets re-inveted in post-tax assets
            if (surplus.IsMoreThanZero())
            {
                deposits.PostTax += surplus;
                surplus = 0;
            }

            adjustedWithdrawals = new(withdrawals.PreTax, withdrawals.PostTax, withdrawals.Cash);
            adjustedDeposits = new(deposits.PostTax, deposits.Cash);

            // Independent validation of math integrity
            VerifyWithdrawals(Jan, Fees, Incomes, Expenses, adjustedWithdrawals, adjustedDeposits, available);

            return true;

            // Try to transfer suggested funds from source to target.
            // If suggested amount is not availabe, transfer available funds.
            static double TryTransferFunds(ref double whatWeNeed, ref double source, ref double target)
            {
                if (whatWeNeed.IsMoreThanZero() && source.IsMoreThanZero())
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
            good = AlmostSame(Incomes.Total() + Withdrawals.Total(), Expenses.Total() + Deposits.Total());
            if (!good) throw new Exception($"Incomes {Incomes.Total():C0} + Withdrawals {Withdrawals.Total():C0} != Expenses {Expenses.Total():C0} + Deposits {Deposits.Total():C0}");

            // Expenses are met. (TODO: Fix rounding error here)
            good = Incomes.Total() + Withdrawals.Total() + 0.01 >= Expenses.Total();
            if (!good) throw new Exception($"Incomes {Incomes.Total():C0} + Withdrawals {Withdrawals.Total():C0} is less than expenses {Expenses.Total():C0}");

            // Per-asset balance agrees: a.Starting - a.Fees - a.Withdrawals = a.Available
            good &= AlmostSame(Jan.PreTax.Amount - Fees.PreTax - Withdrawals.PreTax, Available.PreTax);
            good &= AlmostSame(Jan.PostTax.Amount - Fees.PostTax - Withdrawals.PostTax, Available.PostTax);
            good &= AlmostSame(Jan.Cash.Amount - Fees.Cash - Withdrawals.Cash, Available.Cash);
            if (!good) throw new Exception("Starting - Fees - Withdrals != Available");

            // Either withdrawals or Deposits should be zero.
            good &= Withdrawals.PostTax.AlmostZero() || Deposits.PostTax.AlmostZero();
            good &= Withdrawals.Cash.AlmostZero() || Deposits.Cash.AlmostZero();
            if (!good) throw new Exception($"Meaningless fund transfers. Both withdrawal and deposit can't be positive.");
        }

        //......................................................................
        #region Roundng and validation helpers 
        //......................................................................
        const double TOLLERANCE = 1e-6;

        // static double RoundToCents(this double value) => Math.Round(value, digits: 2, mode: MidpointRounding.AwayFromZero);
        // static double ZeroReset(this double value) => Math.Abs(value) <= TOLLERANCE ? 0.0 : value;
        static bool AlmostSame(double a, double b) => Math.Abs(a - b) < TOLLERANCE;
        static bool IsMoreThanZero(this double value) => value > TOLLERANCE;
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
        #region Temp data structures for tracking and adjusting nunmbers
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
