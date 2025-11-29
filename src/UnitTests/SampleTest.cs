
using NinthBall;

namespace UnitTests
{
    [TestClass]
    public sealed class SampleTest
    {
        [TestMethod]
        public void HelloSimContext()
        {
            const double Initial  = 1000.0;
            const double Alloc    = 0.6;
            const double MaxDrift = 0.03;
            const int    NumYears = 5;

            void SetPlannedWithdrawalAmount(ISimContext ctx) => ctx.PlannedWithdrawalAmount = 1000;
            void SetFees(ISimContext ctx) => ctx.Fees = ctx.JanBalance * 0.09;

            ISimObjective[] myObjectives = 
            [
                FxSimObjective.Create(SetPlannedWithdrawalAmount),
                FxSimObjective.Create(SetFees),
            ];

            var result = myObjectives.RunIteration(1, Initial, Alloc, MaxDrift, NumYears);

            PrintYears(result.ByYear);
        }


        static void PrintYears(IReadOnlyList<SimYear> years)
        {
            foreach(SimYear year in years) 
            {
                Console.WriteLine(year.ToString()); 
            }
        }
    }
}