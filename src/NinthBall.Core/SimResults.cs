
namespace NinthBall.Core
{
    public sealed record SimResult(SimInput Input, IReadOnlyList<string> Strategies, IReadOnlyList<SimIteration> Iterations);

    public sealed record SimIteration(int Index, bool Success, ReadOnlyMemory<SimYear> ByYear);

    public readonly record struct SimYear
    (
        int         Year,
        int         Age,
        Assets      Jan,
        Fees        Fees,
        Taxes       Taxes,
        Incomes     Incomes,
        Expenses    Expenses,
        ROI         ROI,
        Withdrawals Withdrawals,
        Deposits    Deposits,
        Change      Change,
        Assets      Dec,
        Metrics     Metrics
    );

    public readonly record struct Asset(double Amount, double Allocation);

    public readonly record struct Assets(Asset PreTax, Asset PostTax, Asset Cash);

    public readonly record struct Fees(double PreTax, double PostTax);

    //public readonly record struct Taxable(double OrdInc, double DIV, double INT, double LTCG);

    //public readonly record struct Tax(double OrdInc, double DIV, double INT, double LTCG);

    //public readonly record struct TaxRate(double OrdInc, double LTCG);

    //public readonly record struct Taxes(double StandardDeduction, TaxRate TaxRates, Taxable Taxable, Tax Tax);

    public readonly record struct Taxes(Taxes.Inc GrossIncome, Taxes.TD Federal, Taxes.TD State)
    {
        public readonly record struct Inc(double OrdInc, double INT, double DIV, double LTCG) { public readonly double Total => OrdInc + INT + DIV + LTCG; }
        public readonly record struct TR(double OrdInc, double LTCG);
        public readonly record struct TD(double Deduction, double Taxable, TR MarginalRate, double Tax, Inc TaxBreakdown) { public readonly double EffectiveRate => Taxable < 0.01 ? 0.0 : Tax / Taxable; }
        public readonly double Total => Federal.Tax + State.Tax;
        public readonly double EffectiveRate => GrossIncome.Total < 0.01 ? 0.0 : Total / GrossIncome.Total;
    }

    public readonly record struct Incomes(double SS, double Ann);

    public readonly record struct Expenses(double LivExp);

    public readonly record struct Withdrawals(double PreTax, double PostTax, double Cash);

    public readonly record struct Deposits(double PostTax, double Cash);

    public readonly record struct ROI(int LikeYear, double StocksROI, double BondsROI, double InflationRate);

    public readonly record struct Change(double PreTax, double PostTax);

    public readonly record struct Metrics
    (
        // Multiplication factors
        double InflationMultiplier,         // Cumulative inflation multiplier since year #0
        double FedTaxInflationMultiplier,   // Cumulative Federal tax indexing (with lag + ratchet)
        double StateTaxInflationMultiplier, // Cumulative State tax indexing (with lag + ratchet)
        double GrowthMultiplier,            // Nominal growth multiplier since year #0

        // Percentage values
        double PortfolioReturn,         // Portfolio-weighted nominal return for the current year
        double AnnualizedReturn,        // Annualized nominal return (CAGR) since year #0
        double RealAnnualizedReturn     // Inflation-adjusted annualized return since year #0
    )
    {
        // CRITICAL: Multipliers start with 1.0, rest are zero
        public Metrics() : this(1.0, 1.0, 1.0, 1.0, 0.0, 0.0, 0.0) { }
    }

}

// TODO: Adjust Taxes - Include provision for Federal tax and State tax 
// TODO: Track adjusted inflation rate for NJ State tax - 70% lag
// TODO: CHeck if Federal schedule is indexed down on negative inflation years
// TODO: CHeck how negative inflation should be handled for NJ tax inflation rate
// TODO: ??? Property tax deductions
// TODO: Check out Pension exclusion and Pension exclusion cliff.
// TODO: Split Tax calculators to Federal and State tax calculator
// TODO: Consider early design provision for alternate state (PA) tax calculator
// TODO: Anti-gravity made some correction to ChatGPT suggested logic on how tax brackets are applied. Write unit test to validate
