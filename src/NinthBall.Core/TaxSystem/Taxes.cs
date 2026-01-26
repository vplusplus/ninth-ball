

namespace NinthBall.Core
{
    public readonly record struct Taxes(Taxes.GI GrossIncome, Taxes.Tx FederalTax, Taxes.Tx StateTax)
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
            double Gross,        // The Statutory Inclusion Base (Fed AGI or State Gross)
            double Deductions,   // Total subtractors (Exclusions + Exemptions + Deductions)
            double Taxable,      // The amount subject to the rates
            double MTR,          // Marginal rate on ordinary income
            double MTRCapGain,   // Marginal rate on long-term gains
            double Tax           // Total liability in dollars            
        );

        // Total taxes.
        public readonly double Total => FederalTax.Tax + StateTax.Tax;

        // For every $ that came in, what PCT went to taxes
        public readonly double TaxPCT => GrossIncome.Total <= 0.01 ? 0.0 : (FederalTax.Tax + StateTax.Tax) / GrossIncome.Total;

        // For every $ that came in, what PCT went to Federal taxes
        public readonly double TaxPCTFed => GrossIncome.Total <= 0.01 ? 0.0 : FederalTax.Tax / GrossIncome.Total;

        // For every $ that came in, what PCT went to State taxes
        public readonly double TaxPCTState => GrossIncome.Total <= 0.01 ? 0.0 : StateTax.Tax / GrossIncome.Total;
    }
}
