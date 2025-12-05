
namespace UnitTests.V2
{
    /// <summary> 
    /// TotalIncome buckets. Can withdraw. Cannot deposit.
    /// </summary>
    public enum In { 
        SS,                 // TotalIncome from Social security.
        Ann,                // TotalIncome from Annuities
        FourK               // TotalIncome from 401K
    }

    /// <summary>
    /// Expense buckets. Can deposit. Cannot withdraw.
    /// </summary>
    public enum Exp { 
        CYExp,              // Estimated current year expenses (post tax)
        PYTax,              // Taxes on prior year income
        Fees                // Fees payable to managed accounts
    }

    /// <summary>
    /// Can behave as both TotalIncome or Expense. 
    /// </summary>
    public enum IO { 
        Inv,              // Investment account, can consume or invest funds.
        Sav               // Savings account, can withdraw or deposit cash.
    }

    /// <summary>
    /// Tracks planned changes (deltas) to accounts during a simulation year.
    /// All bucket values start at zero and represent planned changes from opening balances.
    /// TotalIncome adds virtual funds, expenses consume funds, and asset operations plan balance changes.
    /// The Residual must reach zero before committing deltas to actual accounts.
    /// </summary>
    /// <param name="Jan4K">Read-only reference: Opening 401K balance (not modified by CYState)</param>
    /// <param name="JanInv">Read-only reference: Opening Investment balance (not modified by CYState)</param>
    /// <param name="JanSav">Read-only reference: Opening Savings balance (not modified by CYState)</param>
    public record CYState(double Jan4K, double JanInv, double JanSav)
    {
        public double Jan4K { get; private set; } = Jan4K;
        public double JanInv { get; private set; } = JanInv;
        public double JanSav { get; private set; } = JanSav;

        readonly Dictionary<In,  double> InDelta  = InitZero(In.SS, In.Ann, In.FourK);
        readonly Dictionary<Exp, double> ExpDelta = InitZero(Exp.CYExp, Exp.PYTax, Exp.Fees);
        readonly Dictionary<IO,  double> IODelta  = InitZero(IO.Inv, IO.Sav);

        public double CashInHand { get; private set; } = 0.0;       // Virtual cash in hand during transaction planning.
        public double TotalIncome => InDelta.Sum(x => x.Value);     // Total income from all income streams
        public double TotalExpenses => ExpDelta.Sum(x => x.Value);  // Total estimated expenses
        public double AssetChanges => IODelta.Sum(x => x.Value);    // Total change in investment and cash assets

        /// <summary>
        /// Checksum - Must always read zero after each transaction.
        /// </summary>
        public double ZeroSum => (TotalIncome + TotalExpenses) - (AssetChanges + CashInHand);  

        /// <summary>
        /// Throws an exception is ZeroSum do not agree.
        /// </summary>
        public double EnsureZeroSum() => Math.Abs(ZeroSum) > 1e-6 ? throw new InvalidOperationException($"ZeroSum is not zero: {ZeroSum}") : ZeroSum;

        /// <summary>
        /// Records income received from the specified source.
        /// Increases the income bucket and adds cash to your hand.
        /// Input amount must be non-negative.
        /// </summary>
        public CYState AddIncome(In bucket, double amount)
        {
            if (amount < 0) throw new ArgumentException("Income must be non-negative.");
            InDelta[bucket] += amount;
            CashInHand += amount;  // Receiving income adds to cash in hand
            return this;
        }

        /// <summary>
        /// Reduces income from the specified source (e.g., correction or adjustment).
        /// Decreases the income bucket and removes cash from your hand.
        /// Input amount must be non-negative.
        /// </summary>
        public CYState ReduceIncome(In bucket, double amount)
        {
            if (amount < 0) throw new ArgumentException("Income reduction must be non-negative.");
            InDelta[bucket] -= amount;
            CashInHand -= amount;  // Reducing income removes from cash in hand
            return this;
        }

        /// <summary>
        /// Records an expense you need to pay.
        /// Increases the expense bucket (stored as negative) and removes cash from your hand.
        /// Input amount must be non-negative.
        /// </summary>
        public CYState AddExpense(Exp bucket, double amount)
        {
            if (amount < 0) throw new ArgumentException("Expense must be non-negative");
            ExpDelta[bucket] -= amount; // enforce negative
            CashInHand -= amount;   // Paying expense removes from cash in hand
            return this;
        }

        /// <summary>
        /// Reduces an expense (e.g., refund or correction).
        /// Decreases the expense bucket and returns cash to your hand.
        /// Input amount must be non-negative.
        /// </summary>
        public CYState ReduceExpense(Exp bucket, double amount)
        {
            if (amount < 0) throw new ArgumentException("Expense reduction must be non-negative");
            ExpDelta[bucket] += amount; 
            CashInHand += amount;   // Refund adds back to cash in hand
            return this;
        }

        /// <summary>
        /// Plans to deposit cash from your hand into the target asset account.
        /// Increases the asset delta and removes cash from your hand.
        /// Input amount must be non-negative.
        /// </summary>
        public double Deposit(IO target, double amount)
        {
            if (amount < 0) throw new ArgumentException("Deposit amount must be non-negative");

            IODelta[target] += amount;
            CashInHand -= amount;   // Depositing removes from cash in hand
            return amount;
        }

        /// <summary>
        /// Plans to withdraw from the source asset account into your hand.
        /// Decreases the asset delta and adds cash to your hand.
        /// Input amount must be non-negative.
        /// </summary>
        public double Withdraw(IO source, double amount)
        {
            if (amount < 0) throw new ArgumentException("Withdrawal amount must be non-negative");

            IODelta[source] -= amount;
            CashInHand += amount;   // Withdrawing adds to cash in hand
            return amount;
        }

        /// <summary>
        /// Records income and immediately pays it toward an expense (net-zero effect on CashInHand).
        /// Input amount must be non-negative.
        /// </summary>
        public void Transfer(In source, Exp target, double amount)
        {
            if (amount < 0) throw new ArgumentException("Transfer amount must be non-negative");
            
            AddIncome(source, amount);
            AddExpense(target, amount);
        }

        /// <summary>
        /// Plans a transfer between asset accounts (net-zero effect on CashInHand).
        /// Input amount must be non-negative.
        /// </summary>
        public void Transfer(IO source, IO target, double amount)
        {
            if (amount < 0) throw new ArgumentException("Transfer amount must be non-negative");

            Withdraw(source, amount);
            Deposit(target, amount);
        }

        public double SS    => InDelta[In.SS];
        public double ANN   => InDelta[In.Ann];
        public double FourK => InDelta[In.FourK];
        public double CYExp => ExpDelta[Exp.CYExp];
        public double PYTax => ExpDelta[Exp.PYTax];
        public double Fees  => ExpDelta[Exp.Fees];
        public double Inv   => IODelta[IO.Inv];
        public double Sav   => IODelta[IO.Sav];

        public void Reset(double jan4K, double janInv, double janSav)
        {
            foreach (var key in InDelta.Keys) InDelta[key] = 0;
            foreach (var key in ExpDelta.Keys) ExpDelta[key] = 0;
            foreach (var key in IODelta.Keys) IODelta[key] = 0;
            CashInHand = 0;

            this.Jan4K = jan4K;
            this.JanInv = janInv;
            this.JanSav = janSav;
        }

        public override string ToString() => $"CashInHand: {CashInHand:F0} Income: {TotalIncome:F0} Expenses: {TotalExpenses:F0} Assets: {AssetChanges:F0}";
        static Dictionary<T, double> InitZero<T>(params T[] keys) where T : notnull => keys.ToDictionary(x => x, x => 0.0);
    }
}
