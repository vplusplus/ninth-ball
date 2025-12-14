
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using System.Net.WebSockets;

namespace NinthBall
{

    internal class TaxStrategy(SimConfig simConfig) : ISimObjective
    {
        readonly Taxes T = simConfig.Taxes;

        int ISimObjective.Order => 10;

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => new Strategy(simConfig.Taxes);
        
        sealed record Strategy(Taxes tx) : ISimStrategy
        {
            const double PCT85 = 0.85;
            const double PCT100 = 0.85;

            void ISimStrategy.Apply(ISimContext context)
            {
                context.Expenses = context.Expenses with
                {
                    PYTax = 0 == context.YearIndex 
                        ? tx.YearZeroTaxAmount 
                        : ComputePriorYearTaxes(context.PriorYears[^1])
                };
            }

            double ComputePriorYearTaxes(SimYear priorYear)
            {
                // Round-UP to a $
                return Math.Ceiling( 0.0
                    + ComputeTax(priorYear.Incomes.SS, PCT85, tx.OrdinaryIncomeTaxRate)
                    + ComputeTax(priorYear.Incomes.Ann, PCT100, tx.OrdinaryIncomeTaxRate)
                    + ComputeTax(priorYear.Withdrawals.PreTax, PCT100, tx.OrdinaryIncomeTaxRate)
                    + ComputeTax(priorYear.Change.Cash, PCT100, tx.OrdinaryIncomeTaxRate)
                    + ComputeTax(priorYear.Change.PostTax, PCT100, tx.CapitalGainsTaxRate)
                 );
            }

            // Some of the gains (taxable income) may be negative.
            // No negative tax amounts. Use Zero.
            static double ComputeTax(double income, double pctIncomeTaxed, double taxRate) => Math.Max(0, income * pctIncomeTaxed * taxRate);
        }

        public override string ToString() => $"Taxes | Ordinary income: {T.OrdinaryIncomeTaxRate:P1} | Capital gains: {T.CapitalGainsTaxRate:P1}";
    }
}

