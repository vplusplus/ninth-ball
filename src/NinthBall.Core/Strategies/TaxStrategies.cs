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

        bool UseFlatTaxRate => TaxOptions.TaxRates.OrdinaryIncome > 0 && TaxOptions.TaxRates.CapitalGains > 0;

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex)
        {
            // The strategy wraps the DI-resolved schedules but allows for flat-rate overrides 
            // from the simulation input (for backwards compatibility/simplicity).
            var fed = UseFlatTaxRate
                ? TaxRateSchedules.Flat(TaxOptions.TaxRates.OrdinaryIncome) 
                : fedSched;

            var ltcg = UseFlatTaxRate
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
                else
                {
                    // Index the tax schedules.
                    var indexedFedTaxRates = Fed.Inflate(context.PriorYear.Metrics.FedTaxInflationMultiplier);
                    var indexedCapGainTaxRates = FedLtcg.Inflate(context.PriorYear.Metrics.FedTaxInflationMultiplier);
                    var indexedStateTaxRates = State.Inflate(context.PriorYear.Metrics.StateTaxInflationMultiplier);

                    context.Taxes = context.PriorYear.ComputePriorYearTaxes
                    (
                        fedTaxRates: indexedFedTaxRates,
                        fedCapGainTaxRates: indexedCapGainTaxRates,
                        stateTaxRates: indexedStateTaxRates,
                        fedDeductions: BaseFedDeduction,
                        stateDeduction: BaseStateExemption
                    );
                }
            }

            //static Taxes ZZFirstYearTaxes(double amount) => new Taxes(
            //    new Taxes.GrossInc(0, 0, 0, 0),
            //    new Taxes.TD(0, 0, new Taxes.TR(0, 0), amount, new Taxes.GrossInc(amount, 0, 0, 0)), // Allocated to Federal for simplicity
            //    new Taxes.TD(0, 0, new Taxes.TR(0, 0), 0, new Taxes.GrossInc(0, 0, 0, 0))
            //).RoundToCents();

            static Taxes FirstYearTaxes(double amount) => new Taxes()
            {
                FederalTax = new() { 
                    Tax = amount,
                }
            };
        }

        public override string ToString() => UseFlatTaxRate
            ? $"Taxes | Ord Income: {TaxOptions.TaxRates.OrdinaryIncome:P0} | Cap Gains: {TaxOptions.TaxRates.CapitalGains:P0}"
            : $"Taxes | Tiered rate using Federal, CapGain and State tax schedules | Brackets are indexed for inflation with lag.";


    }
}