
//namespace NinthBall
//{
//    /// <summary>
//    /// PCT of initial balance, yearly increment and optional periodic reset.
//    /// </summary>
//    public sealed class PCTWithdrawalObjective(SimConfig simConfig) : ISimObjective
//    {
//        readonly SimConfig C = simConfig;
//        public readonly PCTWithdrawal P = simConfig.PCTWithdrawal;

//        int ISimObjective.Order => 0;
        
//        // Expose for optimization objectives
//        public double FirstYearPct => P.FirstYearPct;

//        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => new Strategy(C.InitialFourK.Amount, P.FirstYearPct, P.IncrementPct, P.ResetYears);

//        sealed class Strategy(double startingBalance, double firstYearPct, double yearlyIncrementPct, IReadOnlyList<int> resetYears) : ISimStrategy
//        {
//            double prevYearAmount = 0;

//            void ISimStrategy.Apply(SimContext ctx)
//            {
//                var year = ctx.YearIndex;

//                if (0 == year)
//                {
//                    // First year: Use PCT of initial balance
//                    var from401K = startingBalance * firstYearPct;
//                    ctx.X401K += from401K;
//                    prevYearAmount = from401K;
//                }
//                else if (null != resetYears && resetYears.Contains(year + 1))
//                {
//                    // Reset year: Use PCT of current balance
//                    var from401K = startingBalance * firstYearPct;
//                    ctx.X401K += from401K;
//                    prevYearAmount = from401K;
//                }
//                else
//                {
//                    // Other years: Increment withdrawal amount
//                    var from401K = prevYearAmount * (1 + yearlyIncrementPct);
//                    ctx.X401K += from401K;
//                    prevYearAmount = from401K;
//                }
//            }
//        }

//        public override string ToString() => P.ToString();
//    }

//    /// <summary>
//    /// Pre-calculated withdrawal sequence from an external file.
//    /// </summary>
//    public sealed class PrecalculatedWithdrawalObjective(SimConfig simConfig) : ISimObjective
//    {
//        readonly SimConfig C = simConfig;
//        readonly PrecalculatedWithdrawal P = simConfig.PrecalculatedWithdrawal;

//        public ISimStrategy CreateStrategy(int iterationIndex) => new Strategy(P.WithdrawalSequence);

//        sealed class Strategy(IReadOnlyList<double> sequence) : ISimStrategy
//        {
//            void ISimStrategy.Apply(SimContext context)
//            {
//                var from401K = context.YearIndex >= 0 && context.YearIndex < sequence.Count
//                    ? sequence[context.YearIndex]
//                    : throw new IndexOutOfRangeException($"Year index #{context.YearIndex} is outside the range of this withdrawal strategy.");

//                context.X401K += from401K;
//            }
//        }

//        public override string ToString() => P.ToString();
//    }
//}
