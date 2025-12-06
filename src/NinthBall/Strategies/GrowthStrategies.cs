

// Historical returns:
// REF: https://pages.stern.nyu.edu/~adamodar/New_Home_Page/datafile/histretSP.html?utm_source=chatgpt.com


namespace NinthBall
{
    /// <summary>
    /// Assumes flat growth each iterationYearIndex.
    /// </summary>
    public sealed class FlatGrowthObjective(SimConfig simConfig) : ISimObjective
    {
        // readonly SimConfig C = simConfig;
        readonly FlatGrowth P = simConfig.FlatGrowth;

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex) => new Strategy(P.StocksGrowthRate, P.BondGrowthRate);

        sealed class Strategy(double stocksFlatGrowthPct, double bondsFlatGrowthPct) : ISimStrategy
        {
            readonly YROI ZeroGrowth = new(0, 0, 0);
            readonly YROI OneGrowth = new(0, stocksFlatGrowthPct, bondsFlatGrowthPct);

            void ISimStrategy.Apply(SimContext context)
            {
                context.ROI = OneGrowth;
            }
        }

        public override string ToString() => P.ToString();
    }

    /// <summary>
    /// Use random blocks of historical returns.
    /// </summary>
    public sealed class HistoricalGrowthObjective(SimConfig simConfig) : ISimObjective
    {
        readonly SimConfig C = simConfig;
        readonly HistoricalGrowth P = simConfig.HistoricalGrowth;

        public int MaxIterations => P.UseRandomBlocks ? C.Iterations : P.AllYears.Count - C.NoOfYears + 1;

        ISimStrategy ISimObjective.CreateStrategy(int iterationIndex)
        {
            var iterationRand = new Random(PredictableHashCode.Combine(C.SessionSeed, iterationIndex));

            return P.UseRandomBlocks
                ? new RandomBlocksStrategy(iterationRand, P.AllBlocks, C.NoOfYears, P.NoConsecutiveBlocks)
                : new SequentialHistoryStrategy(P.AllYears, iterationIndex: iterationIndex);
        }

        sealed class RandomBlocksStrategy(Random rand, IReadOnlyList<Block> allBlocks, int numYears, bool noConsecutiveRepetition) : ISimStrategy
        {
            readonly YROI ZeroGrowth = new(0, 0, 0);

            // SampleRandomMovingBlocks rndom blocks, prepare ROI sequence
            readonly YROI[] MyROISequence = Bootstrap.SampleRandomMovingBlocks(rand, allBlocks, numYears,
                noConsecutiveRepetition: noConsecutiveRepetition
            );

            void ISimStrategy.Apply(SimContext context)
            {
                var roi = context.YearIndex >= 0 && context.YearIndex < MyROISequence.Length
                    ? MyROISequence[context.YearIndex]
                    : throw new IndexOutOfRangeException($"Year #{context.YearIndex} is outside the range of this growth strategy");

                context.ROI = roi;
            }
        }

        sealed class SequentialHistoryStrategy(IReadOnlyList<YROI> history, int iterationIndex) : ISimStrategy
        {
            readonly YROI ZeroGrowth = new(0, 0, 0);

            void ISimStrategy.Apply(SimContext context)
            {
                var historyYear = context.IterationIndex + context.YearIndex;

                var roi = historyYear >= 0 && historyYear < history.Count
                    ? history[historyYear]
                    : throw new IndexOutOfRangeException($"Iteration #{iterationIndex} and year #{context.YearIndex} is outside the range of this growth strategy");

                context.ROI = roi;
            }
        }

        public override string ToString() => P.ToString();
    }
}
