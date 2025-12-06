
//namespace NinthBall
//{
//    internal class AnnualFeesObjective(SimConfig simConfig) : ISimObjective
//    {
//        readonly SimConfig C = simConfig;
//        readonly Fees P = simConfig.Fees;

//        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex)
//        {
//            return new Strategy(P);
//        }

//        sealed record Strategy(Fees MyFees) : ISimStrategy
//        {
//            void ISimStrategy.Apply(SimContext context)
//            {
//                // Be considerate with multiple possible fFee strategies/
//                // Don't replace the Fees. Use +=
//                // context.Fees += context.JanBalance * annualFeesPct;
//                var amount = context.JanFourK.Amount * MyFees.Fees401K;
//                context.CurrentYear.AddIncome(In.FourK, amount);
//                context.CurrentYear.AddExpense(Exp.FeesFourK, amount);

//                amount = context.JanFourK.Amount * MyFees.FeesInv;
//                context.CurrentYear.Withdraw(IO.Inv, amount);
//                context.CurrentYear.AddExpense(Exp.FeesInv, amount);
//            }
//        }

//        public override string ToString() => P.ToString();
//    }
//}
