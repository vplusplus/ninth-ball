

namespace NinthBall
{
    /// <summary>
    /// Represents a simulation objective; Provides ISimStrategy for each iteration.
    /// </summary>
    public interface ISimObjective
    {
        int Order { get => 50; }
        ISimStrategy CreateStrategy(int iterationIndex);
    }

    /// <summary>
    /// Strategy that applies a specific simulation objective to a given context.
    /// </summary>
    public interface ISimStrategy 
    {
        void Apply(ISimContext context);
    }

    /// <summary>
    /// Context representing the state of a single iteration during simulation.
    /// </summary>
    public interface ISimContext
    {
        /// <summary> Zero-based index of the current simulation iteration. </summary>
        int IterationIndex { get; }

        /// <summary> Zero-based index of the simulation year within the current iteration. </summary>
        int YearIndex { get; }

        /// <summary> Performance data from prior years. Empty on the first year of an iteration. </summary>
        IReadOnlyList<SimYear> PriorYears { get; }

        /// <summary> Portfolio balance at the start of the current year (January 1st). </summary>
        double JanBalance { get; }

        /// <summary> Portfolio balance after deducting fees and withdrawals (amounts may be adjusted). </summary>
        double AvailableBalance { get; }

        /// <summary> Initial planned withdrawal amount before any adjustments. </summary>
        double PlannedWithdrawalAmount { set; }

        /// <summary> Actual withdrawal amount, potentially adjusted from the planned amount. </summary>
        double WithdrawalAmount { get; set; }

        /// <summary> Fees applicable to the current year. </summary>
        double Fees { get; set; }

        /// <summary> Return on investment for the current year. </summary>
        YROI ROI { set; }
    }

    /// <summary>
    /// Portfolio balance with support for rebalancing, reallocation, withdrawals, and growth.
    /// </summary>
    public record class SimBalance(double InitialBalance, double InitialStockAllocation, double InitialMaxDrift)
    {
        public double StockBalance { get; private set; } = InitialBalance * InitialStockAllocation;
        public double BondBalance { get; private set; } = InitialBalance * (1 - InitialStockAllocation);
        public double CurrentBalance => StockBalance + BondBalance;
        
        public double TargetStockAllocation { get; private set; } = InitialStockAllocation;
        public double CurrentStockAllocation => StockBalance / CurrentBalance;
        public double TargetMaxDrift { get; private set; } = InitialMaxDrift;
        public double CurrentDrift => Math.Abs(CurrentStockAllocation - TargetStockAllocation) * 2;

        public double Reduce(double amount)
        {
            if (amount < 0) throw new ArgumentException("Reduce: Amount must be positive.");
            if (CurrentBalance < amount) throw new InvalidOperationException("Reduce: Can't reduce more than what we have.");

            // If nothing to reduce.
            if (0 == amount) return 0;

            // This is how much we plan to reduce from stock and bond balance.
            double fromStock = amount * TargetStockAllocation;
            double fromBond = amount - fromStock;

            // Try take from correct asset.
            TryTakeFromStock(ref fromStock);
            TryTakeFromBond(ref fromBond);

            // There may be leftover. Try otherway.
            TryTakeFromBond(ref fromStock);
            TryTakeFromStock(ref fromBond);

            // By now, both must be zero.
            if (fromStock + fromBond > 0) throw new InvalidOperationException("SimBalance.Reduce: Unexpected mismatch in Reduce calculation.");
            return amount;

            void TryTakeFromStock(ref double amt)
            {
                var took = Math.Min(amt, StockBalance);
                StockBalance -= took;
                amt -= took;
            }

            void TryTakeFromBond(ref double amt)
            {
                var took = Math.Min(amt, BondBalance);
                BondBalance -= took;
                amt -= took;
            }
        }

        public double Grow(double stocksROI, double bondsROI)
        {
            var stockChange = StockBalance * stocksROI;
            var bondChange = BondBalance * bondsROI;

            StockBalance += stockChange;
            BondBalance += bondChange;

            return stockChange + bondChange;
        }

        public bool Rebalance()
        {
            if (Math.Abs(CurrentDrift) > Math.Abs(TargetMaxDrift))
            {
                var tmpTotalBalance = CurrentBalance;
                StockBalance = tmpTotalBalance * TargetStockAllocation;
                BondBalance  = tmpTotalBalance - StockBalance;
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Reallocate(double newStockPct, double newMaxDrift)
        {
            if (newStockPct < 0.0 || newStockPct > 1.0) throw new ArgumentException("Reallocate: StockAllocationPct must be between 0.0 and 1.0");
            if (newMaxDrift < 0.0 || newMaxDrift > 1.0) throw new ArgumentException("Reallocate: MaxDrift must be between 0.0 and 1.0");

            TargetStockAllocation = newStockPct;
            TargetMaxDrift = newMaxDrift;
            this.Rebalance();
        }

        public override string ToString() => $"{CurrentBalance:C0} = {StockBalance:C0} + {BondBalance:C0}";
    }

    /// <summary>
    /// Implementats ISimContext. Provides state management for a single simulation iteration.
    /// </summary>
    public record SimContext(int IterationIndex, double InitialBalance, double InitialStockAllocation, double InitialMaxDrift) : ISimContext
    {
        static readonly YROI ZeroROI = new(0, 0, 0);

        readonly SimBalance    MyBalance = new(InitialBalance, InitialStockAllocation, InitialMaxDrift);
        readonly List<SimYear> MyPriorYears = [];

        int     _YearIndex = 0;
        double  _PlannedWithdrawal = double.NaN;
        double  _ActualWithdrawal  = double.NaN;
        double  _Fees = 0;
        YROI    _ROI = ZeroROI;

        public int YearIndex => _YearIndex;

        public IReadOnlyList<SimYear> PriorYears => this.MyPriorYears;

        public double JanBalance => MyBalance.CurrentBalance;

        public double AvailableBalance => MyBalance.CurrentBalance
            - (double.IsNaN(_Fees) ? 0 : _Fees)
            - (double.IsNaN(_ActualWithdrawal) ? 0 : _ActualWithdrawal);

        double ISimContext.PlannedWithdrawalAmount
        {
            set => _ActualWithdrawal = _PlannedWithdrawal = double.IsNaN(_PlannedWithdrawal) ? value : throw new Exception("Planned and actual withdrawal already initialized.");
        }

        double ISimContext.WithdrawalAmount
        {
            get => double.IsNaN(_ActualWithdrawal) ? throw new Exception("Planned and actual withdrawal amount not initialized.") : _ActualWithdrawal;
            set => _ActualWithdrawal = value;
        }

        double ISimContext.Fees
        {
            get => _Fees;
            set => _Fees = value;
        }

        YROI ISimContext.ROI
        {
            set => _ROI = value ?? throw new Exception("ROI can't be null.");
        }

        public void StartYear(int yearIndex)
        {
            _YearIndex = yearIndex;
            _PlannedWithdrawal = double.NaN;
            _ActualWithdrawal = double.NaN;
            _Fees = 0;
            _ROI = ZeroROI;

            MyBalance.Rebalance();
        }

        public bool EndYear()
        {
            double janBalance = MyBalance.CurrentBalance;
            double janAlloc = MyBalance.CurrentStockAllocation;
            double changeInValue = 0;

            var success = !double.IsNaN(_ActualWithdrawal) && MyBalance.CurrentBalance >= (_Fees + _ActualWithdrawal);

            if (success)
            {
                MyBalance.Reduce(_Fees);
                MyBalance.Reduce(_ActualWithdrawal);
                changeInValue = null != _ROI ? MyBalance.Grow(_ROI.StocksROI, _ROI.BondROI) : 0;
            }

            MyPriorYears.Add(new SimYear
            (
                YearIndex,
                JanBalance: janBalance,
                JanStockPct: janAlloc,

                PlannedWithdrawal:  success ? _PlannedWithdrawal : 0,
                ActualWithdrawal:   success ? _ActualWithdrawal : 0,
                Fees:               success ? _Fees : 0,
                Change:             success ? changeInValue : 0,
                DecBalance:         success ? MyBalance.CurrentBalance : 0,
                DecStockPct:        success ? MyBalance.CurrentStockAllocation : 0,

                StockROI:           success ? _ROI?.StocksROI ?? 0 : 0,
                BondROI:            success ? _ROI?.BondROI ?? 0 : 0,
                LikeYear:           success ? _ROI?.Year ?? 0 : 0
            ));

            return success;
        }
    }
}
