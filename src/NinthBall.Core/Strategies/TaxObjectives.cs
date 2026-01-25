
namespace NinthBall.Core
{
    abstract class TaxObjective(Initial Initial, TaxRateSchedule Fed, TaxRateSchedule LTCG, TaxRateSchedule State) : ISimObjective
    {
        int ISimObjective.Order => 31;

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex)
        {
            return new Strategy
            (
                YearZeroTaxLiability:   Initial.YearZeroTaxAmount,
                TaxRatesFederal:        Fed,
                TaxRatesLTCG:           LTCG,
                TaxRatesState:          State
            );
        }

        private sealed record Strategy(double YearZeroTaxLiability, TaxRateSchedule TaxRatesFederal, TaxRateSchedule TaxRatesLTCG, TaxRateSchedule TaxRatesState) : ISimStrategy
        {
            void ISimStrategy.Apply(ISimState context)
            {
                if (0 == context.YearIndex)
                {
                    context.Taxes = YearZeroTaxes(YearZeroTaxLiability);
                }
                else
                {
                    context.Taxes = context.PriorYear.ComputePriorYearTaxes
                    (
                        taxRatesFederal: TaxRatesFederal,
                        taxRatesLTCG: TaxRatesLTCG,
                        taxRatesState: TaxRatesState
                    );
                }
            }

            // We do not have any information before year #0
            // Use exact amount as specified as year #0 tax liability
            // BY-DESIGN: All amounts are captured in Federal Ordinary income. Rest of the attributes are irrelevant.
            static Taxes YearZeroTaxes(double amount) => new Taxes()
            {
                FederalTax = new()
                {
                    Tax = amount,
                }
            };
        }
    }

    [StrategyFamily(StrategyFamily.Taxes)] sealed class FlatTaxObjective(Initial Initial, FlatTax FT) : TaxObjective
    ( 
        Initial,
        TaxRateSchedules.Flat(marginalTaxRate: FT.FederalOrdInc, taxDeductions: FT.StandardDeduction),
        TaxRateSchedules.Flat(marginalTaxRate: FT.FederalLTCG, taxDeductions: 0),
        TaxRateSchedules.Flat(marginalTaxRate: FT.State, taxDeductions: FT.StateExemption)
    )
    {
        public override string ToString() => $"Taxes | Fed: {FT.FederalOrdInc:P1} | LTCG: {FT.FederalLTCG:P1} | State: {FT.State:P1} | Standard deduction: {FT.StandardDeduction:C0} | State exemptions: {FT.StateExemption:C0} (indexed)";
    }

    [StrategyFamily(StrategyFamily.Taxes)] sealed class TieredTaxObjective(Initial Initial, TaxRateSchedules TaxSchedules) : TaxObjective(
        Initial,
        TaxSchedules.Federal,
        TaxSchedules.LTCG,
        TaxSchedules.State
    )
    {
        public override string ToString() => $"Taxes | Federal, LTCG and State tax-schedules indexed for inflation | Standard deduction: {TaxSchedules.Federal.TaxDeductions:C0} | State exemptions: {TaxSchedules.State.TaxDeductions:C0} (indexed)";
    }

}


/*
 * 
    sealed record TaxStrategy(double YearZeroTaxLiability, TaxRateSchedule TaxRatesFederal, TaxRateSchedule TaxRatesLTCG, TaxRateSchedule TaxRatesState) : ISimStrategy
    {
        void ISimStrategy.Apply(ISimState context)
        {
            if (0 == context.YearIndex)
            {
                context.Taxes = YearZeroTaxes(YearZeroTaxLiability);
            }
            else
            {
                context.Taxes = context.PriorYear.ComputePriorYearTaxes
                (
                    taxRatesFederal:    TaxRatesFederal,
                    taxRatesLTCG:       TaxRatesLTCG,
                    taxRatesState:      TaxRatesState
                );
            }
        }

        // We do not have any information before year #0
        // Use exact amount as specified as year #0 tax liability
        // BY-DESIGN: All amounts are captured in Federal Ordinary income. Rest of the attributes are irrelevant.
        static Taxes YearZeroTaxes(double amount) => new Taxes()
        {
            FederalTax = new() { 
                Tax = amount,
            }
        };
    }
    [StrategyFamily(StrategyFamily.Taxes)] sealed class ZFlatTaxStrategy( Initial Initial, FlatTax FT) : ISimObjective
    {
        int ISimObjective.Order => 31;

        readonly TaxRateSchedule FlatTaxFederal = TaxRateSchedules.Flat(marginalTaxRate: FT.FederalOrdInc, taxDeductions: FT.StandardDeduction);
        readonly TaxRateSchedule FlatTaxLTCG    = TaxRateSchedules.Flat(marginalTaxRate: FT.FederalLTCG,   taxDeductions: 0);
        readonly TaxRateSchedule FlatTaxState   = TaxRateSchedules.Flat(marginalTaxRate: FT.State,         taxDeductions: FT.StateExemption);

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex)
        {
            return new TaxStrategy
            (
                YearZeroTaxLiability:   Initial.YearZeroTaxAmount,
                TaxRatesFederal:        FlatTaxFederal,
                TaxRatesLTCG:           FlatTaxLTCG,
                TaxRatesState:          FlatTaxState
            );
        }

        public override string ToString() => $"Taxes | Fed: {FT.FederalOrdInc:P1} | LTCG: {FT.FederalLTCG:P1} | State: {FT.State:P1} | Standard deduction: {FT.StandardDeduction:C0} | State exemptions: {FT.StateExemption:C0} (indexed)";
    }
    [StrategyFamily(StrategyFamily.Taxes)] sealed class ZTieredTaxStrategy(Initial Initial, TaxRateSchedules TaxSchedules) : ISimObjective
    {
        int ISimObjective.Order => 31;

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex)
        {
            return new TaxStrategy
            (
                YearZeroTaxLiability:   Initial.YearZeroTaxAmount,
                TaxRatesFederal:        TaxSchedules.Federal,
                TaxRatesLTCG:           TaxSchedules.LTCG,
                TaxRatesState:          TaxSchedules.State
            );
        }

        public override string ToString() => $"Taxes | Federal, LTCG and State tax-schedules indexed for inflation | Standard deduction: {TaxSchedules.Federal.TaxDeductions:C0} | State exemptions: {TaxSchedules.State.TaxDeductions:C0} (indexed)";
    }


*/