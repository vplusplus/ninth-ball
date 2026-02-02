
using Microsoft.Extensions.DependencyInjection;

namespace NinthBall.Core
{
    /// <summary>
    /// Orchestrates tax guesstimation across Federal and State jurisdictions.
    /// </summary>
    public sealed class SamAndHisBrothers
    (
        [FromKeyedServices(TaxAuthority.Federal)] ITaxGuesstimator federalGuesstimator,
        [FromKeyedServices(TaxAuthority.State)] ITaxGuesstimator stateGuesstimator,
        TaxAndMarketAssumptions TAMA
    ) : ITaxSystem
    {
        public Taxes GuesstimateTaxes(SimYear priorYear, TaxRateSchedules Year0TaxRates)
        {
            // Calculate total cash inflow for TaxPCT reporting
            var grossIncome = priorYear.UnadjustedGrossIncomes(TAMA);

            // Each guesstimator extracts what it needs from SimYear
            var federal = federalGuesstimator.GuesstimateTaxes(priorYear, Year0TaxRates);
            var state = stateGuesstimator.GuesstimateTaxes(priorYear, Year0TaxRates);

            return new Taxes(grossIncome, federal, state);
        }
    }
}
