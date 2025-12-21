

namespace NinthBall.Core
{
    sealed class CashBalance : IBalance
    {
        private double _cash;

        public double Amount => _cash;
        public double Allocation => 1.0;

        public void Reset(double initialBalance)
        {
            _cash = initialBalance < 0 ? throw new ArgumentException("Initial balance must be >= 0") : initialBalance;
        }

        public bool Rebalance(double _) => false;
        public bool Reallocate(double _, double __) => false;
        public void Post(double amount) => _cash += amount;

        public double Grow(double ROI)
        {
            var change = Amount * ROI;
            _cash += change;
            
            if (_cash < 0) { change -= _cash; _cash = 0; }
            
            return change;
        }
    }

    sealed class SplitBalance : IBalance
    {
        private double Stocks;
        private double Bonds;
        private double TargetAllocation;

        public double Amount => Stocks + Bonds;
        public double Allocation => 0 == (Stocks + Bonds) ? 0.0 : Stocks / (Stocks + Bonds + Precision.Rate);

        double CurrentDrift => Math.Abs(Allocation - TargetAllocation);

        public void Reset(double initialBalance, double initialAllocation)
        {
            Stocks = initialBalance < 0 ? throw new ArgumentException("Initial balance must be >= 0") : initialBalance * initialAllocation;
            Bonds  = initialBalance * (1 - initialAllocation);
            TargetAllocation = initialAllocation;
        }

        public bool Rebalance(double maxDrift)
        {
            if (CurrentDrift > Math.Abs(maxDrift))
            {
                double tmpAmount = Stocks + Bonds;
                Stocks = tmpAmount * TargetAllocation;
                Bonds  = tmpAmount - Stocks;
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

                withdrawalAmount = Math.Min(withdrawalAmount, Amount);

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
                    if (taking.IsMoreThanZero(Precision.Rate)) { whatIHave -= taking; whatINeed -= taking; }
                }
            }
        }

        public double Grow(double stocksROI, double bondsROI)
        {
            var sChange = Stocks * stocksROI;
            var bChange = Bonds * bondsROI;

            Stocks += sChange;
            Bonds += bChange;

            if (Stocks < 0) { sChange -= Stocks; Stocks = 0; }
            if (Bonds < 0) { bChange -= Bonds; Bonds = 0; }

            return sChange + bChange;
        }
    }

    sealed class SimContext : ISimContext
    {
        // ..........................................
        // Running balances and prior year results
        // ..........................................
        private readonly SplitBalance MyPreTaxBalance  = new();
        private readonly SplitBalance MyPostTaxBalance = new();
        private readonly CashBalance  MyCashBalance = new();
        private Memory<SimYear>       MyPriorYears;

        // ..........................................
        // View of the running balances and prior year results
        // ..........................................
        public IBalance PreTaxBalance  => MyPreTaxBalance;
        public IBalance PostTaxBalance => MyPostTaxBalance;
        public IBalance CashBalance    => MyCashBalance;
        public ReadOnlyMemory<SimYear> PriorYears => MyPriorYears.Slice(0, YearsCompleted);

        public int YearsCompleted { get; private set; }
        public int IterationIndex { get; private set; }
        public int StartAge { get; private set; }

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

        public void Reset(InitialBalance initialBalance, int iterationIndex, int startAge, Memory<SimYear> store)
        {
            MyPreTaxBalance.Reset(initialBalance.PreTax.Amount, initialBalance.PreTax.Allocation);
            MyPostTaxBalance.Reset(initialBalance.PostTax.Amount, initialBalance.PostTax.Allocation);
            MyCashBalance.Reset(initialBalance.Cash.Amount);
            
            IterationIndex = iterationIndex;
            YearIndex = 0;
            StartAge = startAge;
            YearsCompleted = 0;

            MyPriorYears = store;

            Fees = default;
            Incomes = default;
            Expenses = default;
            Withdrawals = default;
            Refills = default;
            ROI = default;
        }

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

        public bool ImplementStrategies()
        {
            var jan = new Assets(AsReadOnly(MyPreTaxBalance), AsReadOnly(MyPostTaxBalance), AsReadOnly(MyCashBalance));

            var success = SimFinalization.FinalizeWithdrawals(
                jan, Fees, Incomes, Expenses, Withdrawals, Refills,
                out var adjustedWithdrawal, out var adjustedDeposits
            );

            if (success)
            {
                MyPreTaxBalance.Post(-Fees.PreTax - adjustedWithdrawal.PreTax);
                MyPostTaxBalance.Post(-Fees.PostTax - adjustedWithdrawal.PostTax);
                MyCashBalance.Post(-Fees.Cash - adjustedWithdrawal.Cash);

                var growth = new Change(
                    MyPreTaxBalance.Grow(ROI.StocksROI, ROI.BondsROI),
                    MyPostTaxBalance.Grow(ROI.StocksROI, ROI.BondsROI),
                    MyCashBalance.Grow(ROI.CashROI)
                );

                MyPostTaxBalance.Post(adjustedDeposits.PostTax);
                MyCashBalance.Post(adjustedDeposits.Cash);

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
