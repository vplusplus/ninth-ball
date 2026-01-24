
namespace NinthBall.Core
{
    internal sealed class Simulation(SimInput Input, InitialBalance InitBalance, SimParams SimParams, SimObjectivesSelector ActiveObjectives)
    {
        public SimResult RunSimulation()
        {
            ArgumentNullException.ThrowIfNull(Input);
            ArgumentNullException.ThrowIfNull(Input.SimParams);
            ArgumentNullException.ThrowIfNull(Input.InitialBalance);
            ArgumentNullException.ThrowIfNull(ActiveObjectives);

            // List of simulation objective, sorted by execution order.
            var orderedObjectives = ActiveObjectives.GetOrderedActiveObjectives();

            // Find max iterations that we can support.
            int maxIterations = Math.Min(SimParams.Iterations, orderedObjectives.Min(x => x.MaxIterations));
            int noOfYears = SimParams.NoOfYears;

            // Only input parameter we may modify...
            if (maxIterations != SimParams.Iterations)
            {
                Input = Input with
                {
                    SimParams = SimParams with
                    {
                        Iterations = maxIterations,
                    }
                };
            }

            // Pre-allocate ONE giant contiguous block of memory for ALL results (total simYears = maxIterations * noOfYears)
            var dataStore = new SimYear[maxIterations * noOfYears];

            // Run iterations in Parallel; Collect results.
            var iterationResults = Enumerable.Range(0, maxIterations)
                .AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .Select(iterationIndex => SimIterationLoop.RunOneIteration(iterationIndex, SimParams, InitBalance, orderedObjectives, dataStore.AsMemory(iterationIndex * noOfYears, noOfYears)))
                .ToList();

            // Sort the iteration results worst to best.
            // Do not use nominal balance for ranking. The variation in InflationRate sequence can skew nominal value.
            // Use real purchaing power to ensure percentiles are monotonic under variable inflation.
            var iterationResultsWorstToBest = iterationResults
                .OrderBy(iter => iter.SurvivedYears)
                .ThenBy(iter => iter.EndingBalanceReal)
                .ToList()
                .AsReadOnly();

            // Extract strategy descriptions
            var strategyDescriptions = orderedObjectives
                .Select(obj => obj.ToString() ?? "Unknown Strategy")
                .ToList()
                .AsReadOnly();

            return new SimResult(
                Input,
                strategyDescriptions,
                iterationResultsWorstToBest
            );
        }
    }
}

