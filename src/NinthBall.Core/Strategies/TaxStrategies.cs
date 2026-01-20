using Microsoft.Extensions.DependencyInjection;

namespace NinthBall.Core
{
    [SimInput(typeof(TaxStrategy), typeof(TaxConfig))]
    sealed class TaxStrategy(
        [FromKeyedServices(TaxScheduleKind.Federal)] TaxRateSchedule fedSched,
        [FromKeyedServices(TaxScheduleKind.FederalLTCG)] TaxRateSchedule fedLtcgSched,
        [FromKeyedServices(TaxScheduleKind.State)] TaxRateSchedule stateSched,
        TaxConfig TaxOptions
    ) : ISimObjective
    {
        int ISimObjective.Order => 31;

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex)
        {
            // The strategy wraps the DI-resolved schedules but allows for flat-rate overrides 
            // from the simulation input (for backwards compatibility/simplicity).
            var fed = TaxOptions.TaxRates.OrdinaryIncome > 0 
                ? TaxRateSchedules.Flat(TaxOptions.TaxRates.OrdinaryIncome) 
                : fedSched;

            var ltcg = TaxOptions.TaxRates.CapitalGains > 0 
                ? TaxRateSchedules.Flat(TaxOptions.TaxRates.CapitalGains) 
                : fedLtcgSched;

            // Federal Standard Deduction Fallback (2026 MFJ logic)
            var fedDeduction = TaxOptions.StandardDeduction > 0 ? TaxOptions.StandardDeduction : 32200.0;
            
            // State Exemption Fallback (NJ logic: 1500 per person, approx 3000 for MFJ)
            var stateExemption = 3000.0; 

            return new Strategy(fed, ltcg, stateSched, TaxOptions.YearZeroTaxAmount, fedDeduction, stateExemption);
        }

        sealed record Strategy(
            TaxRateSchedule Fed, 
            TaxRateSchedule FedLtcg, 
            TaxRateSchedule State, 
            double YearZeroTax,
            double BaseFedDeduction,
            double BaseStateExemption
        ) : ISimStrategy
        {
            void ISimStrategy.Apply(ISimState context)
            {
                if (0 == context.YearIndex)
                {
                    // Year 0 taxes are typically an input (e.g., from a tax return)
                    context.Taxes = FirstYearTaxes(YearZeroTax);
                    return;
                }

                context.Taxes = TaxMath.Calculate(
                    context.PriorYear,
                    Fed,
                    FedLtcg,
                    State,
                    context.PriorYear.Metrics.FedTaxInflationMultiplier,
                    context.PriorYear.Metrics.StateTaxInflationMultiplier,
                    BaseFedDeduction,
                    BaseStateExemption
                );
            }

            static Taxes FirstYearTaxes(double amount) => new Taxes(
                new Taxes.Inc(0, 0, 0, 0),
                new Taxes.TD(0, 0, new Taxes.TR(0, 15), amount, new Taxes.Inc(amount, 0, 0, 0)), // Allocated to Federal for simplicity
                new Taxes.TD(0, 0, new Taxes.TR(0, 0), 0, new Taxes.Inc(0, 0, 0, 0))
            ).RoundToCents();
        }

        public override string ToString() => $"Taxes | Jurisdictional (Fed & State) | DI-Powered";
    }
}