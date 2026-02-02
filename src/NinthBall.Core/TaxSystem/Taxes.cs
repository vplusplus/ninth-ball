

namespace NinthBall.Core
{
    public enum TaxAuthority
    {
        Undefined,
        Federal,
        State
    }

    /// <summary>
    /// Represents prior year earnings, the unadjusted gross incomes from prior year.
    /// </summary>
    public readonly record struct PYEarnings
    (
        int    Age,             // WHY: Earning on 55 is not same as earning at 73
        double PreTaxWDraw,     // Withdrawals from tax deferred account
        double SS,              // Social security income
        double Ann,             // Annuity incomes
        double BondsYield,      // Bonds yield from PostTax accounts
        double Dividends,       // Dividends from PostTax accounts
        double CapGains         // Capital gains from PostTax accounts
    )
    {
        public readonly double Total => PreTaxWDraw + SS + Ann + BondsYield + Dividends + CapGains;
    }


    /// <summary>
    /// Can compute and consolidate tax guesstimation from multiple tax authorities.
    /// </summary>
    public interface ITaxSystem
    {
        Taxes GuesstimateTaxes(SimYear priorYear, TaxRateSchedules Year0TaxRates);
    }

    /// <summary>
    /// Can guesstimate taxes for a specific jurisdiction (Federal, State, City, County).
    /// </summary>
    public interface ITaxAuthority
    {
        Taxes.Tx GuesstimateTaxes(SimYear priorYear, TaxRateSchedules Year0TaxRates);
    }

    public readonly record struct Taxes(Taxes.GI GrossIncome, Taxes.Tx Federal, Taxes.Tx State)
    {
        public readonly record struct GI
        (
            double PreTaxWDraw,     // Withdrawals from tax deferred account
            double SS,              // Social security income
            double Ann,             // Annuity incomes
            double BondsYield,      // Bonds yield from PostTax accounts
            double Dividends,       // Dividends from PostTax accounts
            double CapGains         // Capital gains from PostTax accounts
        )
        {
            public readonly double Total => PreTaxWDraw + SS + Ann + BondsYield + Dividends + CapGains;
        }

        public readonly record struct Tx
        (
            double Gross,           // The Statutory Inclusion Base (Fed AGI or State Gross)
            double Deductions,      // Total subtractors (Exclusions + Exemptions + Deductions)
            double Taxable,         // The amount subject to the rates
            double MTR,             // Marginal rate on ordinary income
            double MTT,             // Indexed marginal rate threshold amount on ordinary income (since we will be indexing)
            double MTRCapGain,      // Marginal rate on long-term gains
            double MTTCapGain,      // Indexed marginal rate threshold amount on long-term gains (since we will be indexing)
            double Tax              // Total liability in dollars            
        );

        // Total taxes.
        public readonly double Total => Federal.Tax + State.Tax;

        // For every $ that came in, what PCT went to taxes
        public readonly double TaxPCT => GrossIncome.Total <= 0.01 ? 0.0 : (Federal.Tax + State.Tax) / GrossIncome.Total;

        // For every $ that came in, what PCT went to Federal taxes
        public readonly double TaxPCTFed => GrossIncome.Total <= 0.01 ? 0.0 : Federal.Tax / GrossIncome.Total;

        // For every $ that came in, what PCT went to State taxes
        public readonly double TaxPCTState => GrossIncome.Total <= 0.01 ? 0.0 : State.Tax / GrossIncome.Total;
    }
}
