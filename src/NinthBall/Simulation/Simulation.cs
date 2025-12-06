

namespace NinthBall
{
    internal static class Simulation
    {
        public static SimResult RunSimulation(SimConfig simConfig)
        {
            ArgumentNullException.ThrowIfNull(simConfig);

            // Create simulation objectives.
            var objectives = simConfig.CreateObjectives();

            // Check if HistoricalReturns with sequential-returns simulation is requested.
            // In such case, we may not meet NumIterations objective.
            // Limit max iterations.
            //var historicalGrowthObjective = objectives.OfType<HistoricalGrowthObjective>().SingleOrDefault();
            //var numIterations = Math.Min(
            //    null != historicalGrowthObjective ? historicalGrowthObjective.MaxIterations : simConfig.Iterations,
            //    simConfig.Iterations
            //);

            var numIterations = simConfig.Iterations;

            // Multiple doubles and ints. Use named-parameters.
            return objectives.RunSimulation(
                initialFourK: simConfig.InitialFourK,
                initialInv: simConfig.InitialInv,
                initialSav: simConfig.InitialSav,
                numYears: simConfig.NoOfYears,
                numIterations: numIterations
            );
        }

        /// <summary>
        /// Runs simulation of the objectives.  
        /// </summary>
        public static SimResult RunSimulation(this IReadOnlyList<ISimObjective> objectives,
            Asset initialFourK, Asset initialInv, Asset initialSav, 
            int numYears, int numIterations
        )
        {
            ArgumentNullException.ThrowIfNull(objectives);

            List<SimIteration> iterationResults = [];

            // Run iterations; Collect results; Sort the results worst-to-best.
            // Question:
            // Iterations are sorted by SurvivedYears and EndingBalance. Is this the only view possible?
            var iterationResultsWorstToBest = Enumerable.Range(0, numIterations)
                .Select(iterationIndex => objectives.RunIteration(
                    iterationIndex: iterationIndex, 
                    initialFourK: initialFourK, 
                    initialInv: initialInv, 
                    initialSav: initialSav, 
                    numYears: numYears
                ))
                .OrderBy(iter => iter.SurvivedYears)
                .ThenBy(iter => iter.EndingBalance)
                .ToList()
                .AsReadOnly();

            // Multiple doubles and ints. Use named-parameters.
            return new SimResult(
                objectives,
                iterationResultsWorstToBest
            );
        }

        /// <summary>
        /// Runs a single iteration of the simulation.
        /// This signature is intended for unit testing. Consider RunSimulation()
        /// </summary>
        public static SimIteration RunIteration(this IReadOnlyList<ISimObjective> objectives,
            int iterationIndex,
            Asset initialFourK, Asset initialInv, Asset initialSav, int numYears
        )
        {
            ArgumentNullException.ThrowIfNull(objectives);

            var strategies = objectives.Select(x => x.CreateStrategy(iterationIndex)).ToList();
            var ctx = new SimContext(iterationIndex, initialFourK, initialInv, initialSav);

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
