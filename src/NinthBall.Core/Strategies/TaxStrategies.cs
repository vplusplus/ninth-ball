using Microsoft.Extensions.DependencyInjection;

namespace NinthBall.Core
{
    [StrategyFamily(StrategyFamily.Taxes)]
    sealed class TaxStrategy
    (
        TaxConfig TaxOptions,
        [FromKeyedServices(TaxScheduleKind.Federal)] TaxRateSchedule taxScheduleFederal,
        [FromKeyedServices(TaxScheduleKind.LTCG   )] TaxRateSchedule taxScheduleLTCG,
        [FromKeyedServices(TaxScheduleKind.State  )] TaxRateSchedule taxScheduleState
    ) : ISimObjective
    {
        int ISimObjective.Order => 31;

        readonly TaxRateSchedule TaxRatesFederal = TaxOptions.UseFlatTaxRates ? TaxRateSchedules.Flat(TaxOptions.FlatTaxRates.FederalOrdInc)   : taxScheduleFederal;
        readonly TaxRateSchedule TaxRatesLTCG    = TaxOptions.UseFlatTaxRates ? TaxRateSchedules.Flat(TaxOptions.FlatTaxRates.FederalLTCG)     : taxScheduleLTCG;
        readonly TaxRateSchedule TaxRatesState   = TaxOptions.UseFlatTaxRates ? TaxRateSchedules.Flat(TaxOptions.FlatTaxRates.State)           : taxScheduleState;

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex)
        {
            return new Strategy
            (
                YearZeroTax:            TaxOptions.YearZeroTaxAmount,
                TaxRatesFederal:        this.TaxRatesFederal,
                TaxRatesLTCG:           this.TaxRatesLTCG,
                TaxRatesState:          this.TaxRatesState,
                Year0StdDeduction:      TaxOptions.StandardDeduction,
                Year0StateExemption:    TaxOptions.StateExemption
            );
        }

        sealed record Strategy(double YearZeroTax, 
            TaxRateSchedule TaxRatesFederal, TaxRateSchedule TaxRatesLTCG, TaxRateSchedule TaxRatesState, 
            double Year0StdDeduction,double Year0StateExemption
        ) : ISimStrategy
        {
            void ISimStrategy.Apply(ISimState context)
            {
                if (0 == context.YearIndex)
                {
                    context.Taxes = FirstYearTaxes(YearZeroTax);
                    return;
                }
                else
                {
                    context.Taxes = context.PriorYear.ComputePriorYearTaxes
                    (
                        taxRatesFederal:    TaxRatesFederal,
                        taxRatesLTCG:       TaxRatesLTCG,
                        taxRatesState:      TaxRatesState,
                        year0StdDeductions:  Year0StdDeduction,
                        year0StateExemptions:    Year0StateExemption
                    );
                }
            }

            static Taxes FirstYearTaxes(double amount) => new Taxes()
            {
                FederalTax = new() { 
                    Tax = amount,
                }
            };
        }

        public override string ToString() => TaxOptions.UseFlatTaxRates
            ? $"Taxes | Ord Income: {TaxOptions.FlatTaxRates.FederalOrdInc:P1} | Cap Gains: {TaxOptions.FlatTaxRates.FederalLTCG:P1} | State: {TaxOptions.FlatTaxRates.State:P1}"
            : $"Taxes | Tiered rate using Federal, CapGain and State tax schedules | Brackets are indexed for inflation with lag.";
    }
}