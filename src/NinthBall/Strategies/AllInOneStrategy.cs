
namespace NinthBall
{
    /// <summary>
    /// PCT of initial balance, yearly increment and optional periodic reset.
    /// </summary>
    public sealed class AllInOneStrategy(SimConfig simConfig) : ISimObjective
    {
        int ISimObjective.Order => 0;

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => new Strategy(simConfig);

        sealed class Strategy(SimConfig simConfig) : ISimStrategy
        {
            double expenses = 120000;
            double from401k = simConfig.InitialFourK.Amount * 5 / 100;
            double ss = 0;
            double ann = 0;

            void ISimStrategy.Apply(SimContext ctx)
            {
                var year = ctx.YearIndex;
                
                if (year > 0)
                {
                    var priorYear = ctx.PriorYears[^1];
                    ctx.PYTax = (priorYear.X401K * 0.85 * 0.22) + (priorYear.XInv * 0.15);
                    ctx.PYFees = Math.Max(0, priorYear.DecInv * 0.9 / 100);
                }

                if (year == 11)
                {
                    ss = 72000;
                    ann = 53000;
                }

                ss = ctx.SSIncome = ss * 1.02;
                ann = ctx.AnnIncome = ann * 1;

                expenses = ctx.CYExp = expenses * 1.02;
                from401k = ctx.X401K = from401k * 1.02;
            }
        }

        public override string ToString() => $"Test consolidated strategy";
    }
}
