
namespace NinthBall.Core
{
    sealed class Simulation(SimParams SimParams, Initial InitBalance, SimObjectivesSelector ActiveObjectives, TaxAndMarketAssumptions TAMA)
    {
        public SimResult RunSimulation()
        {
            ArgumentNullException.ThrowIfNull(SimParams);
            ArgumentNullException.ThrowIfNull(InitBalance);
            ArgumentNullException.ThrowIfNull(ActiveObjectives);

            // List of simulation objective, sorted by execution order.
            var orderedObjectives = ActiveObjectives.GetOrderedActiveObjectives();

            // Find max iterations that we can support.
            int maxIterations = Math.Min(SimParams.Iterations, orderedObjectives.Min(x => x.MaxIterations));
            int noOfYears = SimParams.NoOfYears;

            // The only input parameter we may modify...
            if (maxIterations != SimParams.Iterations)
            {
                SimParams = SimParams with
                {
                    Iterations = maxIterations,
                };
            }

            // Pre-allocate ONE giant contiguous block of memory for ALL results (total simYears = maxIterations * noOfYears)
            var dataStore = new SimYear[maxIterations * noOfYears];

            // Run iterations in Parallel; Collect results.
            var iterationResults = Enumerable.Range(0, maxIterations)
                .AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .Select(iterationIndex => SimIterationLoop.RunOneIteration(iterationIndex, SimParams, InitBalance, orderedObjectives, TAMA, dataStore.AsMemory(iterationIndex * noOfYears, noOfYears)))
                .ToList();

            // Sort the iteration results worst to best.
            // Do not use nominal balance for ranking.
            // The variation in InflationRate sequence can skew nominal value.
            // Use real purchaing power to ensure percentiles are monotonic under variable inflation.
            var iterationResultsWorstToBest = iterationResults
                .OrderBy(iter => iter.SurvivedYears)
                .ThenBy(iter => iter.EndingBalanceReal)
                .ToList()
                .AsReadOnly();

            // Extract strategy descriptions
            // Cosmetic: Push NoOp objective messages to bottom, like a foot note.
            var strategyDescriptions = orderedObjectives
                .OrderBy(x => x is NoOpObjective ? 1 : 0)
                .Select(obj => obj.ToString() ?? "Unknown Strategy")
                .ToList()
                .AsReadOnly();

            return new SimResult(
                SimParams,
                strategyDescriptions,
                iterationResultsWorstToBest
            );
        }
    }
}

