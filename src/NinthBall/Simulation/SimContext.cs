

namespace NinthBall
{
    sealed record CashBalance(double InitialBalance) : IBalance
    {
        double Cash = InitialBalance < 0 ? throw new ArgumentException("Initial balance must be >= 0") : InitialBalance;

        public double Amount => Cash;
        public double Allocation => 1.0;

        public bool Rebalance(double _) => false;                     // Single asset. Nothing to Rebalance.
        public bool Reallocate(double _, double __) => false;         // Single asset. Nothing to Reallocate.

        public void Post(double amount) => Cash += amount;

        public double Grow(double ROI)
        {
            var change = Amount * ROI;
            Cash += change;
            return change;
        }
    }

    sealed record SplitBalance(double InitialBalance, double InitialAllocation) : IBalance
    {
        double Stocks = InitialBalance < 0 ? throw new ArgumentException("Initial balance must be >= 0") : InitialBalance * InitialAllocation;
        double Bonds  = InitialBalance * (1 - InitialAllocation);
        double TargetAllocation = InitialAllocation;

        double CurrentAllocation => 0 == (Stocks + Bonds) ? 0.0 : Stocks / (Stocks + Bonds + 0.0001);
        double CurrentDrift => Math.Abs(CurrentAllocation - TargetAllocation);

        public double Amount => Stocks + Bonds;
        public double Allocation => CurrentAllocation;

        public bool Rebalance(double maxDrift)
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
            return Rebalance(maxDrift);
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
                if (withdrawalAmount > Amount + 0.001) throw new InvalidOperationException($"Can't withdraw more than what we have | Available: {Amount:C0} | Requested: {withdrawalAmount:C0}");

                // TODO: Revisit rounding issues
                withdrawalAmount = Math.Min(withdrawalAmount, Amount);

                // This is how much we plan to reduce from stock and bond balance.
                double fromStock = withdrawalAmount * TargetAllocation;
                double fromBond  = withdrawalAmount - fromStock;

                // Try to taking from correct asset.
                TryTake(ref Stocks, ref fromStock);
                TryTake(ref Bonds, ref fromBond);

                // There may be insufficient funds. Try otherway.
                TryTake(ref Bonds, ref fromStock);
                TryTake(ref Stocks, ref fromBond);

                // We had enough funds. By now, both must be zero.
                if (fromStock + fromBond > 0.0001) throw new InvalidOperationException("SplitBalance.Withdraw() - Unexpected mismatch in calculation.");

                // Clean-up near zero
                ResetNearZero(ref Stocks);
                ResetNearZero(ref Bonds);
                return;

                static void TryTake(ref double whatIHave, ref double whatINeed)
                {
                    var taking = Math.Min(whatIHave, whatINeed);

                    if (taking > 0.00001)
                    {
                        whatIHave -= taking;
                        whatINeed -= taking;
                    }
                }

                static void ResetNearZero(ref double amount)
                {
                    if (Math.Abs(amount) < 1e-6) amount = 0;
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

    sealed record SimContext(InitialBalance InitPortfolio, int IterationIndex, int StartAge) : ISimContext
    {
        // ..........................................
        // Running balances and prior year results
        // ..........................................
        private readonly SplitBalance  MyPreTaxBalance  = new(InitPortfolio.PreTax.Amount, InitPortfolio.PreTax.Allocation);
        private readonly SplitBalance  MyPostTaxBalance = new(InitPortfolio.PostTax.Amount, InitPortfolio.PostTax.Allocation);
        private readonly CashBalance   MyCashBalance    = new(InitPortfolio.Cash.Amount);
        private readonly List<SimYear> MyPriorYears     = new();

        // ..........................................
        // View of the running balances and prior year results
        // ..........................................
        public IBalance PreTaxBalance  => MyPreTaxBalance;
        public IBalance PostTaxBalance => MyPostTaxBalance;
        public IBalance CashBalance    => MyCashBalance;
        public IReadOnlyList<SimYear> PriorYears => MyPriorYears;

        // ..........................................
        // Current year 
        // ..........................................
        public int YearIndex { get; private set; }
        public int Age => StartAge + YearIndex;
        public Fees Fees { get; set; }
        public Incomes Incomes { get; set; }
        public Expenses Expenses { get; set; }
        public Withdrawals Withdrawals { get; set; }
        public Deposits Refills { get; set; }
        public ROI ROI { get; set; }
        // ..........................................

        private Assets GetPortfolioSnapshot() => new Assets(AsReadOnly(MyPreTaxBalance), AsReadOnly(MyPostTaxBalance), AsReadOnly(MyCashBalance));

        public void BeginNewYear(int yearIndex)
        {
            YearIndex = yearIndex;

            // Reset working area (erase memory of prior year choices) 
            Fees        = default;
            Incomes     = default;
            Expenses    = default;
            Withdrawals = default;
            Refills     = default;
            ROI         = default;
        }

        public bool ImplementStrategies()
        {
            var janBalance = GetPortfolioSnapshot();

            var success = SimFinalization.FinalizeWithdrawals(
                janBalance,
                Fees,
                Incomes, Expenses,
                Withdrawals, Refills,
                out var adjustedWithdrawals,
                out var adjustedDeposits
            );

            if (success)
            {
                //................................................
                // Apply the changes to the portfolio
                //................................................

                // Fees goes first
                MyPreTaxBalance.Post(-Fees.PreTax);
                MyPostTaxBalance.Post(-Fees.PostTax);
                MyCashBalance.Post(-Fees.Cash);

                // Take withdrawals
                MyPreTaxBalance.Post(-adjustedWithdrawals.PreTax);
                MyPostTaxBalance.Post(-adjustedWithdrawals.PostTax);
                MyCashBalance.Post(-adjustedWithdrawals.Cash);

                // Some time after Jan 1st - Apply ROI
                var growth = new Change(
                    PreTax: MyPreTaxBalance.Grow(ROI.StocksROI, ROI.BondsROI),
                    PostTax: MyPostTaxBalance.Grow(ROI.StocksROI, ROI.BondsROI),
                    Cash: MyCashBalance.Grow(ROI.CashROI)
                );

                // Apply deposits (refills/fund-transfers)
                // REVISIT: Why deposits on Dec 31st if funds are idling through the year?
                // BECAUSE: If excess fund is coming from PreTax, may be we wont have that cash on Jan 1st
                // BUT - What if the take the money, market crashes and xfer? This may artificially lower the risk?
                // BUT - What if the returns are very good - This may artificially lower the returns?
                MyPostTaxBalance.Post(adjustedDeposits.PostTax);
                MyCashBalance.Post(adjustedDeposits.Cash);
                //................................................

                // Ending balance
                var decBalance = GetPortfolioSnapshot();

                MyPriorYears.Add(new SimYear(
                    Year: YearIndex, Age: Age, 
                    janBalance, 
                    Fees, Incomes, Expenses, adjustedWithdrawals, adjustedDeposits, ROI, growth, 
                    decBalance
                ));
            }
            else
            {
                MyPriorYears.Add(new SimYear()
                {
                    Year = YearIndex,
                    Age  = Age,
                    Jan  = janBalance,
                });
            }

            return success;
        }

        private static Asset AsReadOnly(IBalance x) => new(x.Amount, x.Allocation);

    }

}
