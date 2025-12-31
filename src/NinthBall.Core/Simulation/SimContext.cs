

namespace NinthBall.Core
{
    /// <summary>
    /// Maintains running balance of cash asset.
    /// </summary>
    sealed class CashBalance : IBalance
    {
        private double Cash;
        double IBalance.Amount => Cash;
        double IBalance.Allocation => 1.0;                        // Single asset. Allocation is not applicable
        public void Reset(double initialBalance) => Cash = initialBalance < 0 ? throw new ArgumentException("Initial balance must be >= 0") : initialBalance;
        public bool Rebalance(double _) => false;               // Single asset. Nothing to rebalance
        public bool Reallocate(double _, double __) => false;   // Single asset. Allocation is not applicable
        public void Post(double amount)
        {
            if (amount > 0) Deposit(amount); else Withdraw(Math.Abs(amount));
            return;

            void Deposit(double depositAmount) 
            {
                Cash += depositAmount;
            }

            void Withdraw(double withdrawalAmount) 
            {
                if (withdrawalAmount > Cash  + Precision.Amount) throw new Exception($"Cannot withdraw more than what we have | Available: {Cash:C0} | Requested: {withdrawalAmount:C0}");
                Cash -= withdrawalAmount;
                Cash = Cash.ResetNearZero(Precision.Amount);
            }
        }

        public double Grow(double ROI)
        {
            var change = Math.Round(Cash * ROI);

            Cash += change;
            Cash = Cash.ResetNearZero(Precision.Amount);
            
            return change;
        }
    }

    /// <summary>
    /// Maintains running balance of split asset (stocks and bonds).
    /// </summary>
    sealed class SplitBalance : IBalance
    {
        private double Stocks;
        private double Bonds;
        private double TargetAllocation;

        public double Amount => Stocks + Bonds;

        public double Allocation => 0 == Stocks ? 0.0 : 0 == Bonds ? 1.0 : Stocks / (Stocks + Bonds + Precision.Rate);

        double CurrentDrift => Math.Abs(Allocation - TargetAllocation);

        public void Reset(double initialBalance, double initialAllocation)
        {
            Stocks = initialBalance < 0 ? throw new ArgumentException("Initial balance must be >= 0") : initialBalance * initialAllocation;
            Bonds  = initialBalance * (1 - initialAllocation);
            TargetAllocation = initialAllocation;
        }

        public bool Rebalance(double maxDrift)
        {
            if (Math.Abs(CurrentDrift) > Math.Abs(maxDrift))
            {
                double tmpAmount = Stocks + Bonds;
                Stocks = tmpAmount * TargetAllocation;
                Bonds  = tmpAmount - Stocks;

                Stocks = Stocks.ResetNearZero(Precision.Amount);
                Bonds  = Bonds.ResetNearZero(Precision.Amount);
                return true;
            }
            return false;
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

                var stockChange = depositAmount * TargetAllocation;
                var bondChange  = depositAmount - stockChange;

                Stocks += stockChange;
                Bonds  += bondChange;
            }

            void Withdraw(double withdrawalAmount)
            {
                if (0 == withdrawalAmount) return;
                if (withdrawalAmount > Amount + Precision.Amount) throw new Exception($"Cannot withdraw more than what we have | Available: {Amount:C0} | Requested: {withdrawalAmount:C0}");

                double fromStock = withdrawalAmount * TargetAllocation;
                double fromBond  = withdrawalAmount - fromStock;

                TryTake(ref Stocks, ref fromStock);
                TryTake(ref Bonds,  ref fromBond);
                TryTake(ref Bonds,  ref fromStock);
                TryTake(ref Stocks, ref fromBond);

                Stocks = Stocks.ResetNearZero(Precision.Amount);
                Bonds  = Bonds.ResetNearZero(Precision.Amount);

                static void TryTake(ref double whatIHave, ref double whatINeed)
                {
                    var taking = Math.Min(whatIHave, whatINeed);
                    if (taking.IsMoreThanZero(Precision.Amount)) 
                    { 
                        whatIHave -= taking; 
                        whatINeed -= taking; 
                    }
                }
            }
        }

        public double Grow(double stocksROI, double bondsROI)
        {
            var sChange = Math.Round(Stocks * stocksROI);
            var bChange = Math.Round(Bonds * bondsROI);

            Stocks += sChange;
            Bonds  += bChange;

            Stocks = Stocks.ResetNearZero(Precision.Amount);
            Bonds  = Bonds.ResetNearZero(Precision.Amount);

            return sChange + bChange;
        }
    }

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

        // ..........................................
        // View of the running balances and prior year results
        // ..........................................
        public IBalance PreTaxBalance  => MyPreTaxBalance;
        public IBalance PostTaxBalance => MyPostTaxBalance;
        public IBalance CashBalance    => MyCashBalance;
        public ReadOnlyMemory<SimYear> PriorYears => MyPriorYears.Slice(0, YearsCompleted);

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
        public Deposits Refills { get; set; }
        public ROI ROI { get; set; }
        // ..........................................

        /// <summary>
        /// SimCOntext instances are pooled and re-used.
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
            Refills = default;
            ROI = default;
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
            Refills = default;
            ROI = default;
        }

        /// <summary>
        /// Validate and apply strategy recommendations.
        /// Returns false if the model failed.
        /// </summary>
        public bool ImplementStrategies()
        {
            // Take snapshot of jan 1st balance
            var jan = new Assets(AsReadOnly(MyPreTaxBalance), AsReadOnly(MyPostTaxBalance), AsReadOnly(MyCashBalance));

            // Validate and adjust withdrawals and deposits
            var success = SimFinalization.FinalizeWithdrawals
            (
                jan, 
                Fees, Incomes, Expenses, Withdrawals, Refills,
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
                var dec = new Assets(AsReadOnly(MyPreTaxBalance), AsReadOnly(MyPostTaxBalance), AsReadOnly(MyCashBalance));

                MyPriorYears.Span[YearIndex] = new SimYear(YearIndex, Age, jan, Fees, Incomes, Expenses, adjustedWithdrawal, adjustedDeposits, ROI, growth, dec);
            }
            else
            {
                MyPriorYears.Span[YearIndex] = new SimYear() { Year = YearIndex, Age = Age, Jan = jan };
            }

            YearsCompleted++;
            return success;
        }

        private static Asset AsReadOnly(IBalance x) => new(x.Amount, x.Allocation);
    }
}
