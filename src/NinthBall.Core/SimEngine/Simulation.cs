using System.Collections.ObjectModel;

namespace NinthBall.Core
{
    internal sealed class Simulation(SimInput Input, InitialBalance InitPortfolio, SimParams SimParams, IEnumerable<ISimObjective> Objectives)
    {
        // A pool of SimContext instances, reset & reused for each iteration.
        private readonly ObjectPool<SimContext> SimContextPool = new(() => new SimContext());
 
        public SimResult RunSimulation()
        {
            ArgumentNullException.ThrowIfNull(Input);
            ArgumentNullException.ThrowIfNull(Input.SimParams);
            ArgumentNullException.ThrowIfNull(Input.InitialBalance);
            ArgumentNullException.ThrowIfNull(Objectives);

            // List of simulation objective, sorted by execution order.
            ReadOnlyCollection<ISimObjective> orderedObjectives = Objectives.OrderBy(x => x.Order).ToList().AsReadOnly();

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
                .Select(iterationIndex => RunOneIteration(
                    iterationIndex, 
                    orderedObjectives,
                    dataStore.AsMemory(iterationIndex * noOfYears, noOfYears))
                )
                .ToList();

            // Sort the iteration results worst to best.
            var iterationResultsWorstToBest = iterationResults
                .OrderBy(iter => iter.SurvivedYears)
                .ThenBy(iter => iter.EndingBalance)
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

        private SimIteration RunOneIteration(int iterationIndex, ReadOnlyCollection<ISimObjective> orderedObjectives, Memory<SimYear> myPrivateSliceOfMemory)
        {
            // Rent a context from the pool.
            using var lease = SimContextPool.Rent();
            var ctx = lease.Instance;

            // Reset the context for this iteration.
            ctx.Reset(InitPortfolio, iterationIndex, SimParams.StartAge, myPrivateSliceOfMemory);

            // Strategies can be stateful; Create new set of strategies for each iteration.
            var strategies = orderedObjectives.Select(x => x.CreateStrategy(iterationIndex)).ToList();

            // Assess each year.
            var success = false;
            for (int yearIndex = 0; yearIndex < SimParams.NoOfYears; yearIndex++)
            {
                // Process next year.
                ctx.BeginNewYear(yearIndex);
                foreach (var strategy in strategies) strategy.Apply(ctx);
                success = ctx.ImplementStrategies();

                if (!success) break;
            }

            return new(iterationIndex, success, ctx.PriorYears);
        }
    }
}
