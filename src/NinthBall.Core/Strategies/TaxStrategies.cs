
namespace NinthBall.Core
{
    [SimInput(typeof(TaxStrategy), typeof(Taxes))]
    sealed class TaxStrategy(Taxes Options) : ISimObjective
    {
        int ISimObjective.Order => 31;

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex)
        {
            ThrowIfZero(Options.YearZeroTaxAmount, "YearZeroTaxAmount");
            ThrowIfZero(Options.TaxRates.OrdinaryIncome, "TaxRates.OrdinaryIncome");
            ThrowIfZero(Options.TaxRates.CapitalGains, "TaxRates.CapitalGains");

            return new Strategy(Options);
        } 
        
        sealed record Strategy(Taxes TX) : ISimStrategy
        {
            const double HundredPCT     = 1.00;
            const double EightyFivePCT  = 0.85;

            const double TaxableSS  = EightyFivePCT;
            const double TaxableAnn = HundredPCT;

            void ISimStrategy.Apply(ISimContext context)
            {
                context.Expenses = context.Expenses with
                {
                    PYTax = 0 == context.YearIndex 
                        ? TX.YearZeroTaxAmount 
                        : ComputePriorYearTaxes(context.PriorYears.Span[^1])
                };
            }

            double ComputePriorYearTaxes(SimYear priorYear)
            {
                // Round-UP to a $
                return Math.Ceiling( 0.0

                    // Ordinary incomes
                    + ComputeTax(priorYear.Incomes.SS,          TaxableSS,  TX.TaxRates.OrdinaryIncome)
                    + ComputeTax(priorYear.Incomes.Ann,         TaxableAnn, TX.TaxRates.OrdinaryIncome)
                    + ComputeTax(priorYear.Withdrawals.PreTax,  HundredPCT, TX.TaxRates.OrdinaryIncome)
                    + ComputeTax(priorYear.Change.Cash,         HundredPCT, TX.TaxRates.OrdinaryIncome)
                    
                    // Capital gains
                    + ComputeTax(priorYear.Change.PostTax,      HundredPCT, TX.TaxRates.CapitalGains)
                 );

                // Some of the gains (taxable income) may be negative.
                // Taxes can't be negative. If the pyAmount is negative, compute as ZERO tax.
                static double ComputeTax(double income, double pctIncomeTaxed, double taxRate) => Math.Max(0, income * pctIncomeTaxed * taxRate);
            }
        }

        static void ThrowIfZero(double d, string name) { if (d <= 0) throw new Exception($"{name} cannot be zero. Use some very low value, but not zero, to model zero-tax scenario"); }

        public override string ToString() => $"Taxes | Ordinary: {Options.TaxRates.OrdinaryIncome:P1} | CapGains: {Options.TaxRates.CapitalGains:P1}";
    }
}

