
namespace NinthBall.Core
{
    [StrategyFamily(StrategyFamily.Taxes)] 
    sealed class FlatTaxObjective(Initial Initial, ITaxSystem TheTaxSystem, FlatTax FT) : ISimObjective, ISimStrategy
    {
        readonly TaxRateSchedules Year0TaxRates = new TaxRateSchedules
        (
            TaxRateSchedules.Flat(FT.FederalOrdInc, taxDeductions: FT.StandardDeduction),
            TaxRateSchedules.Flat(FT.FederalLTCG,   taxDeductions: 0),
            TaxRateSchedules.Flat(FT.State,         taxDeductions: FT.StateExemption)
        );

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => this;

        void ISimStrategy.Apply(ISimState context)
        {
            context.Taxes = 0 == context.YearIndex
                ? TaxSystemShared.YearZeroTaxes(Initial.YearZeroTaxAmount)
                : TheTaxSystem.GuesstimateTaxes(context.PriorYear, Year0TaxRates);
        }

        public override string ToString() => $"Taxes | Fed: {FT.FederalOrdInc:P1} | LTCG: {FT.FederalLTCG:P1} | State: {FT.State:P1} | Standard deduction: {FT.StandardDeduction:C0} | State exemptions: {FT.StateExemption:C0} (indexed)";

    }

    [StrategyFamily(StrategyFamily.Taxes)] 
    sealed class TieredTaxObjective(Initial Initial, ITaxSystem TheTaxSystem, TaxRateSchedules Year0TaxRates) : ISimObjective, ISimStrategy
    {
        // int ISimObjective.Order => 31;

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => this;

        void ISimStrategy.Apply(ISimState context)
        {
            context.Taxes = 0 == context.YearIndex
                ? TaxSystemShared.YearZeroTaxes(Initial.YearZeroTaxAmount)
                : TheTaxSystem.GuesstimateTaxes(context.PriorYear, Year0TaxRates);
        }

        public override string ToString() => $"Taxes | Federal, LTCG and State tax-schedules indexed for inflation | Standard deduction: {Year0TaxRates.Federal.TaxDeductions:C0} | State exemptions: {Year0TaxRates.State.TaxDeductions:C0} (indexed)";
    }

}
