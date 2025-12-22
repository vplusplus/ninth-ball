namespace NinthBall.Core
{
    internal sealed class Simulation(SimParams MySimParams, InitialBalance InitPortfolio, IEnumerable<ISimObjective> Objectives)
    {
        private readonly ObjectPool<SimContext> SimContextPool = new(() => new SimContext());
        private readonly IReadOnlyList<ISimObjective> OrderedObjectives = Objectives.OrderBy(x => x.Order).ToList();

        public SimResult RunSimulation()
        {
            ArgumentNullException.ThrowIfNull(MySimParams);
            ArgumentNullException.ThrowIfNull(InitPortfolio);
            ArgumentNullException.ThrowIfNull(Objectives);

            // Find max iterations that we can support.
            int maxIterations = Math.Min(MySimParams.Iterations, OrderedObjectives.Min(x => x.MaxIterations));
            int noOfYears = MySimParams.NoOfYears;

            // Pre-allocate ONE giant contiguous block of memory for ALL results (total simYears = maxIterations * noOfYears)
            var dataStore = new SimYear[maxIterations * noOfYears];

            // Run iterations in Parallel; Collect results.
            var iterationResults = Enumerable.Range(0, maxIterations)
                .AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .Select(iterationIndex => RunOneIteration(
                    iterationIndex, 
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
            var strategyDescriptions = OrderedObjectives
                .Select(obj => obj.ToString() ?? "Unknown Strategy")
                .ToList()
                .AsReadOnly();

            return new SimResult(
                strategyDescriptions,
                iterationResultsWorstToBest
            );
        }

        private SimIteration RunOneIteration(int iterationIndex, Memory<SimYear> myPrivateSliceOfMemory)
        {
            // Rent a context from the pool.
            using var lease = SimContextPool.Rent();
            var ctx = lease.Instance;

            // Reset the context for this iteration.
            ctx.Reset(InitPortfolio, iterationIndex, MySimParams.StartAge, myPrivateSliceOfMemory);

            // Strategies can be stateful; Create new set of strategies for each iteration.
            var strategies = OrderedObjectives.Select(x => x.CreateStrategy(iterationIndex)).ToList();

            // Assess each year.
            var success = false;
            for (int yearIndex = 0; yearIndex < MySimParams.NoOfYears; yearIndex++)
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
