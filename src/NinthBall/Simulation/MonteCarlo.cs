

namespace NinthBall
{
    internal static class MonteCarlo
    {
        public static SimResult RunSimulation(SimConfig simConfig)
        {
            ArgumentNullException.ThrowIfNull(simConfig);

            // Consult SimConfig.
            // Create simulation objectives.
            var objectives = simConfig.CreateObjectives();

            // Check if HistoricalReturns with sequential block simulation is requested.
            // In such case, we may not meet NumIterations objective
            // Limit max iterations.
            var historicalGrowthObjective = objectives.OfType<HistoricalGrowthObjective>().SingleOrDefault();
            var maxIterations = null != historicalGrowthObjective ? historicalGrowthObjective.MaxIterations : simConfig.Iterations;

            return objectives.RunSimulation(
                simConfig.StartingBalance,
                simConfig.StocksAllocationPct,
                simConfig.MaxDrift,
                simConfig.NoOfYears,
                maxIterations
            );
        }

        public static SimResult RunSimulation(this IReadOnlyList<ISimObjective> objectives,
            double initialBalance, double initialAllocation, double initialMaxDrift, int numYears,
            int numIterations
        )
        {
            ArgumentNullException.ThrowIfNull(objectives);

            objectives = objectives.OrderBy(x => x.Order).ToList().AsReadOnly();

            List<SimIteration> iterationResults = [];

            for (int iterationIndex = 0; iterationIndex < numIterations; iterationIndex++)
                iterationResults.Add(
                    objectives.RunIteration(iterationIndex, initialBalance, initialAllocation, initialMaxDrift, numYears)
                );

            var iterationResultsWorstToBest = iterationResults
                .OrderBy(x => x.SurvivedYears)
                .ThenBy(x => x.EndingBalance)
                .ToList()
                .AsReadOnly();

            return new SimResult(
                StartingBalance: initialBalance,
                InitialAllocation: initialAllocation,
                NoOfYears: numYears,
                objectives,
                iterationResultsWorstToBest
            );
        }

        public static SimIteration RunIteration(this IReadOnlyList<ISimObjective> objectives, 
            int iterationIndex, double initialBalance, double initialAllocation, double initialMaxDrift,
            int numYears
        )
        {
            ArgumentNullException.ThrowIfNull(objectives);

            var strategies = objectives.Select(x => x.CreateStrategy(iterationIndex)).ToList();
            var ctx = new SimContext(iterationIndex, initialBalance, initialAllocation, initialMaxDrift);

            bool success = false;

            for (int yearIndex = 0; yearIndex < numYears; yearIndex++)
            {
                ctx.StartYear(yearIndex);
                foreach (var strategy in strategies) strategy.Apply(ctx);
                success = ctx.EndYear();

                if (!success) break;
            }

            return new(iterationIndex, success, ctx.PriorYears);
        }
    }
}
