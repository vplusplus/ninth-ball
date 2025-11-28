

namespace NinthBall
{
    /// <summary>
    /// represents a simulation objective. 
    /// Provides strategy, aligned with the simulation objective.
    /// </summary>
    public interface ISimObjective
    {
        int Order { get => 50; }
        ISimStrategy CreateStrategy(int iterationIndex);
    }

    /// <summary>
    /// Strategy that implements a simulation objective.
    /// </summary>
    public interface ISimStrategy 
    {
        void Apply(ISimContext context);
    }

    /// <summary>
    /// Context of a simulation-iteration.
    /// </summary>
    public interface ISimContext
    {
        /// <summary> Zero based index of a simulation-iteration </summary>
        int IterationIndex { get; }

        /// <summary> Zero based index of simulation year with-in an iteration. </summary>
        int YearIndex { get; }

        /// <summary> Performance in prior years. Will be an empty list on first year. </summary>
        IReadOnlyList<SimYear> PriorYears { get; }

        /// <summary> Portfolio balance as on Jan this year. </summary>
        double JanBalance { get; }

        /// <summary> Portfolio balance less fees and known withdrawals (which might change) </summary>
        double AvailableBalance { get; }

        /// <summary> Originally planned withdrawal amount. </summary>
        double PlannedWithdrawalAmount { set; }

        /// <summary> Adjusted withdrawal amount. </summary>
        double WithdrawalAmount { get; set; }

        /// <summary> Any applicable fees. </summary>
        double Fees { get; set; }

        /// <summary> Estimated ROI this year. </summary>
        YROI ROI { set; }
    }

    /// <summary>
    /// Portfolio balance.
    /// Can rebalance, reallocate, withdraw and grow
    /// </summary>
    public record class SimBalance(double InitialBalance, double InitialStockPct, double InitialMaxDrift)
    {
        public double StockBalance { get; private set; } = InitialBalance * InitialStockPct;
        public double BondBalance { get; private set; } = InitialBalance * (1 - InitialStockPct);
        public double CurrentBalance => StockBalance + BondBalance;
        
        public double TargetStockPct { get; private set; } = InitialStockPct;
        public double CurrentStockPct => StockBalance / CurrentBalance;
        public double TargetMaxDrift { get; private set; } = InitialMaxDrift;
        public double CurrentDrift => Math.Abs(CurrentStockPct - TargetStockPct) * 2;

        public double Reduce(double amount)
        {
            if (amount < 0) throw new ArgumentException("Reduce: Amount must be positive.");
            if (CurrentBalance < amount) throw new InvalidOperationException("Reduce: Can't reduce more than what we have.");

            // If nothing to reduce
            if (0 == amount) return 0;

            // This is how much we plan to reduce from stock and bond balance
            double fromStock = amount * TargetStockPct;
            double fromBond = amount - fromStock;

            // Try take from correct asset
            TryTakeFromStock(ref fromStock);
            TryTakeFromBond(ref fromBond);

            // There may be leftover. Try otherway
            TryTakeFromBond(ref fromStock);
            TryTakeFromStock(ref fromBond);

            // By now, both must be zero
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
                StockBalance = tmpTotalBalance * TargetStockPct;
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

            TargetStockPct = newStockPct;
            TargetMaxDrift = newMaxDrift;
            this.Rebalance();
        }

        public override string ToString() => $"{CurrentBalance:C0} = {StockBalance:C0} + {BondBalance:C0}";
    }

    /// <summary>
    /// Provides context of one iteration.
    /// </summary>
    public record SimContext(int IterationIndex, double InitialBalance, double InitialStockAllocationPct, double InitialMaxDrift) : ISimContext
    {
        readonly SimBalance    _Balance = new(InitialBalance, InitialStockAllocationPct, InitialMaxDrift);
        readonly List<SimYear> _PriorYears = [];

        int     _YearIndex = 0;
        double  _PlannedWithdrawal = double.NaN;
        double  _ActualWithdrawal = double.NaN;
        double  _Fees = double.NaN;
        YROI    _ROI = null!;

        public int YearIndex => _YearIndex;

        public IReadOnlyList<SimYear> PriorYears => _PriorYears;

        public double JanBalance => _Balance.CurrentBalance;

        public double AvailableBalance => _Balance.CurrentBalance
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
            //get => _ROI;
            set => _ROI = value ?? throw new Exception("ROI can't be null.");
        }

        public void StartYear(int yearIndex)
        {
            _YearIndex = yearIndex;
            _PlannedWithdrawal = double.NaN;
            _ActualWithdrawal = double.NaN;
            _Fees = 0;
            _ROI = null!;

            _Balance.Rebalance();
        }

        public bool EndYear()
        {
            double janBalance = _Balance.CurrentBalance;
            double janAlloc = _Balance.CurrentStockPct;
            double changeInValue = 0;

            var success = !double.IsNaN(_ActualWithdrawal) && _Balance.CurrentBalance >= _Fees + _ActualWithdrawal;

            if (success)
            {
                _Balance.Reduce(_Fees);
                _Balance.Reduce(_ActualWithdrawal);
                changeInValue = null != _ROI ? _Balance.Grow(_ROI.StocksROI, _ROI.BondROI) : 0;
            }

            _PriorYears.Add(new SimYear
            (
                YearIndex,
                JanBalance: janBalance,
                JanStockPct: janAlloc,

                PlannedWithdrawal:  success ? _PlannedWithdrawal : 0,
                ActualWithdrawal:   success ? _ActualWithdrawal : 0,
                Fees:               success ? _Fees : 0,
                Change:             success ? changeInValue : 0,
                DecBalance:         success ? _Balance.CurrentBalance : 0,
                DecStockPct:        success ? _Balance.CurrentStockPct : 0,

                StockROI:           success ? _ROI?.StocksROI ?? 0 : 0,
                BondROI:            success ? _ROI?.BondROI ?? 0 : 0,
                LikeYear:           success ? _ROI?.Year ?? 0 : 0
            ));

            return success;
        }
    }

}
