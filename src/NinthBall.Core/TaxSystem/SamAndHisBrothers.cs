
using Microsoft.Extensions.DependencyInjection;

namespace NinthBall.Core
{
    /// <summary>
    /// Orchestrates tax guesstimation across Federal and State jurisdictions.
    /// </summary>
    public sealed class SamAndHisBrothers
    (
        [FromKeyedServices(TaxAuthority.Federal)] ITaxAuthority federalGuesstimator,
        [FromKeyedServices(TaxAuthority.State)] ITaxAuthority stateGuesstimator
    ) : ITaxSystem
    {
        public Taxes GuesstimateTaxes(PYEarnings pyEarnings, InflationIndex inflationIndex, TaxRateSchedules Year0TaxRates)
        {
            // Consult tax authorities, guesstimate tax liabilities.
            return new Taxes
            (
                PYEarnings: pyEarnings, 
                Federal:    federalGuesstimator.GuesstimateTaxes(pyEarnings, inflationIndex, Year0TaxRates),
                State:      stateGuesstimator.GuesstimateTaxes(pyEarnings, inflationIndex, Year0TaxRates)
            );
        }
    }
}
