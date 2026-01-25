
namespace NinthBall.Core
{
    abstract class TaxObjective(Initial Initial, TaxRateSchedules Y0TaxRates, TaxAndMarketAssumptions TAMA) : ISimObjective, ISimStrategy
    {
        int ISimObjective.Order => 31;

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => this;

        void ISimStrategy.Apply(ISimState context)
        {
            context.Taxes = 0 == context.YearIndex
                ? YearZeroTaxes(Initial.YearZeroTaxAmount)
                : context.PriorYear.ComputePriorYearTaxes(Y0TaxRates, TAMA);
        }

        // We do not have any information before year #0
        // Use exact amount as specified as year #0 tax liability
        // BY-DESIGN: All amounts are captured in Federal Ordinary income. Rest of the attributes are irrelevant.
        static Taxes YearZeroTaxes(double amount) => new() { FederalTax = new() { Tax = amount, } };
    }

    [StrategyFamily(StrategyFamily.Taxes)] 
    sealed class FlatTaxObjective(Initial Initial, FlatTax FT, TaxAndMarketAssumptions TAMA) : TaxObjective
    ( 
        Initial,
        new TaxRateSchedules
        (
            TaxRateSchedules.Flat(FT.FederalOrdInc, taxDeductions: FT.StandardDeduction),
            TaxRateSchedules.Flat(FT.FederalLTCG,   taxDeductions: 0),
            TaxRateSchedules.Flat(FT.State,         taxDeductions: FT.StateExemption)
        ),
        TAMA
    )
    {
        public override string ToString() => $"Taxes | Fed: {FT.FederalOrdInc:P1} | LTCG: {FT.FederalLTCG:P1} | State: {FT.State:P1} | Standard deduction: {FT.StandardDeduction:C0} | State exemptions: {FT.StateExemption:C0} (indexed)";
    }

    [StrategyFamily(StrategyFamily.Taxes)] 
    sealed class TieredTaxObjective(Initial Initial, TaxRateSchedules Y0TaxRates, TaxAndMarketAssumptions TAMA) : TaxObjective( Initial, Y0TaxRates, TAMA)
    {
        public override string ToString() => $"Taxes | Federal, LTCG and State tax-schedules indexed for inflation | Standard deduction: {Y0TaxRates.Federal.TaxDeductions:C0} | State exemptions: {Y0TaxRates.State.TaxDeductions:C0} (indexed)";
    }

}
