


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
        void Apply(SimContext context);
    }

    public record Asset(double Amount, double Allocation, double Drift);

    /// <summary>
    /// Represents a portfolio balance, optionally split between two asset classes (Stocks and Bonds)
    /// </summary>
    public record class AssetBalance(Asset InitialAssetAllocation)
    {
        public double StockBalance { get; private set; } = InitialAssetAllocation.Amount * InitialAssetAllocation.Allocation;
        public double BondBalance { get; private set; } = InitialAssetAllocation.Amount * (1-InitialAssetAllocation.Allocation);
        public double CurrentBalance => StockBalance + BondBalance;
        public double TargetAllocation { get; private set; } = InitialAssetAllocation.Allocation;
        public double CurrentAllocation => StockBalance / (CurrentBalance + 0.0000001);
        public double TargetMaxDrift { get; private set; } = InitialAssetAllocation.Drift;
        public double CurrentDrift => Math.Abs(CurrentAllocation - TargetAllocation) * 2;

        public bool Rebalance()
        {
            if (Math.Abs(CurrentDrift) > Math.Abs(TargetMaxDrift))
            {
                var tmpTotalBalance = CurrentBalance;
                StockBalance = tmpTotalBalance * TargetAllocation;
                BondBalance = tmpTotalBalance - StockBalance;
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Reallocate(double newAllocation, double newMaxDrift)
        {
            if (newAllocation < 0.0 || newAllocation > 1.0) throw new ArgumentException("Stock allocation PCT must be between 0.0 and 1.0");
            if (newMaxDrift < 0.0 || newMaxDrift > 1.0) throw new ArgumentException("MaxDrift must be between 0.0 and 1.0");

            TargetAllocation = newAllocation;
            TargetMaxDrift = newMaxDrift;
            this.Rebalance();
        }

        public void Post(double amount)
        {
            if (0 == amount) return;
            else if (double.IsNaN(amount)) throw new Exception("Invalid amount.");
            else if (amount > 0.0) Deposit(amount); 
            else Withdraw(Math.Abs(amount));

            void Deposit(double amount)
            {
                var stockChange = amount * TargetAllocation;
                var bondChange = amount - stockChange;

                StockBalance += stockChange;
                BondBalance += bondChange;
            }

            void Withdraw(double amount)
            {
                if (amount < 0) throw new ArgumentException("Withdrawal amount must be positive.");
                if (CurrentBalance < amount) throw new InvalidOperationException("Can't withdraw more than what we have.");

                // If nothing to reduce.
                if (0 == amount) return;

                // This is how much we plan to reduce from stock and bond balance.
                double fromStock = amount * TargetAllocation;
                double fromBond = amount - fromStock;

                // Try take from correct asset.
                TryTakeFromStock(ref fromStock);
                TryTakeFromBond(ref fromBond);

                // There may be leftover. Try otherway.
                TryTakeFromBond(ref fromStock);
                TryTakeFromStock(ref fromBond);

                // By now, both must be zero.
                if (fromStock + fromBond > 0.01) throw new InvalidOperationException("SimBalance.Reduce: Unexpected mismatch in Reduce calculation.");
                return;

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
        }

        public double Grow(double stocksROI, double bondsROI)
        {
            var stockChange = StockBalance * stocksROI;
            var bondChange = BondBalance * bondsROI;

            StockBalance += stockChange;
            BondBalance += bondChange;

            return stockChange + bondChange;
        }

        public override string ToString() => $"{CurrentBalance:C0} = {StockBalance:C0} + {BondBalance:C0}";
    }

    public record SimContext(int IterationIndex, Asset InitialFourK, Asset InitialInv, Asset InitialSav)
    {
        static readonly YROI ZeroROI = new(0, 0, 0);

        readonly List<SimYear> MyPriorYears = [];
        readonly AssetBalance  CurrentFourK  = new (InitialFourK);
        readonly AssetBalance  CurrentInv    = new (InitialInv);
        readonly AssetBalance  CurrentSav    = new (InitialSav);

        public int YearIndex  { get; private set; } = 0;
        public IReadOnlyList<SimYear> PriorYears => this.MyPriorYears;

        public double CYExp;
        public double PYTax;
        public double PYFees;

        public double SSIncome;
        public double AnnIncome;

        public double X401K;
        public double XInv;
        public double XSav;

        public YROI ROI = ZeroROI;

        public void StartYear(int yearIndex)
        {
            CurrentFourK.Rebalance();
            CurrentInv.Rebalance(); 
            CurrentSav.Rebalance();

            YearIndex = yearIndex;
            CYExp = PYTax = PYFees = X401K = XInv = XSav = 0;
            ROI = ZeroROI;
        }

        public bool EndYear()
        {
            double jan401K  = CurrentFourK.CurrentBalance;
            double janInv = CurrentInv.CurrentBalance;
            double janSav = CurrentSav.CurrentBalance;

            var success = (CYExp + PYTax + PYFees - SSIncome - AnnIncome + 1) < (jan401K + janInv);

            if (success)
            {
                // This is how much we need to cover expenses.
                double pendingExp = CYExp + PYTax + PYFees - SSIncome - AnnIncome;
                double from401K = 0;
                double fromInv = 0;

                if (pendingExp > 0)
                {
                    var take = Math.Min(CurrentFourK.CurrentBalance, X401K);
                    CurrentFourK.Post(-take);
                    pendingExp -= take;
                    from401K += take;
                }
                if (pendingExp > 0)
                {
                    var take = Math.Min(CurrentInv.CurrentBalance, XInv);
                    CurrentInv.Post(-take);
                    pendingExp -= take;
                    fromInv += take;
                }
                if (pendingExp > 0)
                {
                    var take = Math.Min(CurrentInv.CurrentBalance, pendingExp);
                    CurrentInv.Post(-take);
                    pendingExp -= take;
                    fromInv += take;
                }
                if (pendingExp > 0)
                {
                    var take = Math.Min(CurrentFourK.CurrentBalance, pendingExp);
                    CurrentFourK.Post(-take);
                    pendingExp -= take;
                    from401K += take;
                }
                if (pendingExp > 0)
                {
                    throw new Exception("Simulation logic error: Unable to cover expenses despite success condition.");
                }

                var changeIn401K = CurrentFourK.Grow(ROI.StocksROI, ROI.BondROI);
                var changeInInv = CurrentInv.Grow(ROI.StocksROI, ROI.BondROI);

                double surplus = (SSIncome + AnnIncome + from401K + fromInv) - (CYExp + PYTax + PYFees);
                if (surplus > 0)
                {
                    // We withdrew more than what we need.
                    // Return excess to investments.
                    CurrentInv.Post(surplus);
                }

                var dec401K = CurrentFourK.CurrentBalance;
                var decInv = CurrentInv.CurrentBalance;
                var decSav = CurrentSav.CurrentBalance;

                MyPriorYears.Add(new SimYear
                (
                    Year: YearIndex,

                    Jan401K: jan401K,
                    JanInv: janInv,
                    JanSav: janSav,

                    PYTaxes: PYTax,
                    PYFees: PYFees,
                    CYExp: CYExp,

                    SS: SSIncome,
                    ANN: AnnIncome,
                    X401K: from401K,
                    XInv: fromInv,
                    XSav: 0,

                    Surplus: surplus,
                    Change401K: changeIn401K,
                    ChangeInv: changeInInv,
                    ChangeSav: 0,

                    Dec401K: dec401K,
                    DecInv: decInv,
                    DecSav: decSav,

                    LikeYear: ROI.Year
                ));
            }
            else
            {
                MyPriorYears.Add(new SimYear
                (
                    Year: YearIndex,
                    Jan401K: jan401K,
                    JanInv: janInv,
                    JanSav: janSav,

                    SS: 0,
                    ANN: 0,
                    X401K: 0,
                    XInv: 0,
                    XSav: 0,
                    Surplus: 0,
                    Change401K: 0,
                    ChangeInv: 0,
                    ChangeSav: 0,
                    Dec401K: 0,
                    DecInv: 0,
                    DecSav: 0,
                    PYTaxes: 0,
                    PYFees: 0,
                    CYExp: 0,
                    LikeYear: 0
                ));
            }

            return success;
        }
        
    }
}


// Context may require
//  InitialBalance (PreTax, PostTax, NoTax)
//  CurrentBalance (PreTax, PostTax, NoTax)
//  PYBalance (PreTax, PostTax, NoTax)
//  
// Strategies will provide
//  Exp (a.k.a withdrawl)
//  Withdrawals from 401K, SS, Ann
//  Withdrawals/Deposits on Inv and Savings
//  Fees are (Investment) deducted on source - Currently its handled as expense - Change design
//  ROI on 401K, Inv and Savings


//  Sequence:
//  Set PYTax        <-- TaxStrategies : ExpenseStrategy
//  Set CYExp        <-- CashStrategy  : ExpenseStrategy
//  Set PYFees       <-- FeesStrategy  : ExpenseStrategy
//  Set SS Income    <-- SS Strategy   : Income strategy
//  Set Ann Income   <-- Ann Strategy  : Income  strategy
//  Set 401K Draw    <-- WithdrawalStrategy : Income strategy


/*
     /// <summary>
    /// Portfolio balance with support for rebalancing, reallocation, withdrawals, and growth.
    /// </summary>
    public record class SimBalance(double Amount, double InitialAllocation, double InitialMaxDrift)
    {
        public double StockBalance { get; private set; } = Amount * InitialAllocation;
        public double BondBalance { get; private set; } = Amount * (1 - InitialAllocation);
        public double CurrentBalance => StockBalance + BondBalance;
        
        public double TargetAllocation { get; private set; } = InitialAllocation;
        public double CurrentAllocation => StockBalance / CurrentBalance;
        public double TargetMaxDrift { get; private set; } = InitialMaxDrift;
        public double CurrentDrift => Math.Abs(CurrentAllocation - TargetAllocation) * 2;

        public double Reduce(double amount)
        {
            if (amount < 0) throw new ArgumentException("Reduce: Amount must be positive.");
            if (CurrentBalance < amount) throw new InvalidOperationException("Reduce: Can't reduce more than what we have.");

            // If nothing to reduce.
            if (0 == amount) return 0;

            // This is how much we plan to reduce from stock and bond balance.
            double fromStock = amount * TargetAllocation;
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
                StockBalance = tmpTotalBalance * TargetAllocation;
                BondBalance  = tmpTotalBalance - StockBalance;
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Reallocate(double newAllocation, double newMaxDrift)
        {
            if (newAllocation < 0.0 || newAllocation > 1.0) throw new ArgumentException("Reallocate: StockAllocationPct must be between 0.0 and 1.0");
            if (newMaxDrift < 0.0 || newMaxDrift > 1.0) throw new ArgumentException("Reallocate: MaxDrift must be between 0.0 and 1.0");

            TargetAllocation = newAllocation;
            TargetMaxDrift = newMaxDrift;
            this.Rebalance();
        }

        public override string ToString() => $"{CurrentBalance:C0} = {StockBalance:C0} + {BondBalance:C0}";
    }

*/

/*
 * 
     /// <summary>
    /// Context representing the state of a single iteration during simulation.
    /// </summary>
    public interface SimContext
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
    /// Implementats SimContext. Provides state management for a single simulation iteration.
    /// </summary>
    public record SimContext(int IterationIndex, double Amount, double InitialAllocation, double InitialMaxDrift) : SimContext
    {
        static readonly YROI ZeroROI = new(0, 0, 0);

        readonly SimBalance    MyBalance = new(Amount, InitialAllocation, InitialMaxDrift);
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

        double SimContext.PlannedWithdrawalAmount
        {
            set => _ActualWithdrawal = _PlannedWithdrawal = double.IsNaN(_PlannedWithdrawal) ? value : throw new Exception("Planned and actual withdrawal already initialized.");
        }

        double SimContext.WithdrawalAmount
        {
            get => double.IsNaN(_ActualWithdrawal) ? throw new Exception("Planned and actual withdrawal amount not initialized.") : _ActualWithdrawal;
            set => _ActualWithdrawal = value;
        }

        double SimContext.Fees
        {
            get => _Fees;
            set => _Fees = value;
        }

        YROI SimContext.ROI
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
            double janAlloc = MyBalance.CurrentAllocation;
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
                DecStockPct:        success ? MyBalance.CurrentAllocation : 0,

                StockROI:           success ? _ROI?.StocksROI ?? 0 : 0,
                BondROI:            success ? _ROI?.BondROI ?? 0 : 0,
                LikeYear:           success ? _ROI?.Year ?? 0 : 0
            ));

            return success;
        }
    }
*/