
namespace NinthBall.Core
{
    /// <summary>
    /// Represents running-balance tracked by SimContext.
    /// </summary>
    internal interface IBalance
    {
        double Amount { get; }
        double Allocation { get; }
        bool Rebalance(double maxDrift);
        bool Reallocate(double newAllocation, double maxDrift);
    }

    /// <summary>
    /// Maintains running balance of cash asset.
    /// </summary>
    sealed class CashBalance : IBalance
    {
        private double Cash;

        // Current balance
        double IBalance.Amount => Cash;

        // Single asset. Allocation is not applicable
        double IBalance.Allocation => 1.0;

        // Initialize
        public void Reset(double initialBalance) => Cash = initialBalance < 0 ? throw new ArgumentException("Initial balance must be >= 0") : initialBalance;

        // Single asset. Nothing to rebalance
        public bool Rebalance(double _) => false;

        // Single asset. Allocation is not applicable
        public bool Reallocate(double _, double __) => false;

        // Accept the change, reset near zero values.
        public void Post(double amount)
        {
            if (amount > 0) Deposit(amount); else Withdraw(Math.Abs(amount));
            return;

            void Deposit(double depositAmount)
            {
                Cash += depositAmount;
                ResetNearZeroAndKillFractions();
            }

            void Withdraw(double withdrawalAmount)
            {
                if (withdrawalAmount > Cash + Precision.Amount) throw new Exception($"Cannot withdraw more than what we have | Available: {Cash:C0} | Requested: {withdrawalAmount:C0}");
                Cash -= withdrawalAmount;
                ResetNearZeroAndKillFractions();
            }
        }

        public double Grow(double ROI)
        {
            var change = Math.Round(Cash * ROI);

            Cash += change;
            ResetNearZeroAndKillFractions();

            return change;
        }

        void ResetNearZeroAndKillFractions()
        {
            // Almost zero is as good as zero
            Cash = Cash.ResetNearZero(Precision.Amount);

            // Keep cents, kill fractional-cents.
            Cash = Math.Round(Cash, 2);
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
            Bonds = initialBalance * (1 - initialAllocation);
            ResetNearZeroAndKillFractions();

            TargetAllocation = initialAllocation;
        }

        public bool Rebalance(double maxDrift)
        {
            if (Math.Abs(CurrentDrift) > Math.Abs(maxDrift))
            {
                double tmpAmount = Stocks + Bonds;
                Stocks = tmpAmount * TargetAllocation;
                Bonds = tmpAmount - Stocks;
                ResetNearZeroAndKillFractions();

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
                var bondChange = depositAmount - stockChange;

                Stocks += stockChange;
                Bonds += bondChange;
                ResetNearZeroAndKillFractions();
            }

            void Withdraw(double withdrawalAmount)
            {
                if (0 == withdrawalAmount) return;
                if (withdrawalAmount > Amount + Precision.Amount) throw new Exception($"Cannot withdraw more than what we have | Available: {Amount:C0} | Requested: {withdrawalAmount:C0}");

                // Withdrawal implies selling assets.
                // Real-world withdrawal my be optmized for taxes & fees.
                // For our modelling, keeping it simple.
                // Start with TargetAllocation as a guide.
                double fromStock = withdrawalAmount * TargetAllocation;
                double fromBond = withdrawalAmount - fromStock;

                // Try correct asset.
                // If required, try the other asset.
                TryTake(ref Stocks, ref fromStock);
                TryTake(ref Bonds, ref fromBond);
                TryTake(ref Bonds, ref fromStock);
                TryTake(ref Stocks, ref fromBond);

                ResetNearZeroAndKillFractions();

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
            Bonds += bChange;
            ResetNearZeroAndKillFractions();

            return sChange + bChange;
        }

        void ResetNearZeroAndKillFractions()
        {
            // Almost zero is as good as zero
            Stocks = Stocks.ResetNearZero(Precision.Amount);
            Bonds = Bonds.ResetNearZero(Precision.Amount);

            // Keep cents, kill fractional-cents.
            Stocks = Math.Round(Stocks, 2);
            Bonds = Math.Round(Bonds, 2);
        }
    }
}
