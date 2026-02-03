
namespace NinthBall.Core
{
    static class TaxSystemShared
    {
        // We do not have any prior year information on year #0
        // Tax liability is accepted as user input.
        // BY-DESIGN: All amounts are captured in Federal Ordinary income. Rest of the attributes are irrelevant.
        public static Taxes YearZeroTaxes(double amount) => new() 
        { 
            Federal = new() { 
                Tax = amount, 
            } 
        };

        public static PYEarnings UnadjustedEarnings(this SimYear simYear, TaxAndMarketAssumptions TAMA)
        {
            return new PYEarnings
            (
                Age:            simYear.Age,
                PreTaxWDraw:    simYear.Withdrawals.PreTax,
                SS:             simYear.Incomes.SS,
                Ann:            simYear.Incomes.Ann,
                BondsYield:     simYear.Jan.PostTax.BondsAmount * TAMA.TypicalBondCouponYield,
                Dividends:      simYear.Jan.PostTax.StocksAmount * TAMA.TypicalStocksDividendYield,
                CapGains:       simYear.Withdrawals.PostTax + Math.Max(0.0, -simYear.Rebalanced.PostTax.StocksChange)
            )
            .RoundToCents();
        }

    }
}
