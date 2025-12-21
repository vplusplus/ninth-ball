namespace NinthBall.Core
{
    internal sealed class Simulation(
        SimParams MySimParams,
        InitialBalance InitPortfolio,
        IEnumerable<ISimObjective> allObjectives)
    {
        private readonly ObjectPool<SimContext> SimContextPool = new(() => new SimContext());
        private readonly IReadOnlyList<ISimObjective> _objectives = allObjectives.OrderBy(x => x.Order).ToList();

        public SimResult RunSimulation()
        {
            // Consult current objectives. Find max iterations that we can support.
            int maxIterations = Math.Min(MySimParams.Iterations, _objectives.Min(x => x.MaxIterations));
            int noOfYears = MySimParams.NoOfYears;

            // Pre-allocate ONE giant contiguous block of memory for ALL results.
            var dataStore = new SimYear[maxIterations * noOfYears];

            // Run iterations in Parallel; Collect results.
            var iterationResults = Enumerable.Range(0, maxIterations)
                .AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .Select(iterationIndex =>
                {
                    var mySlice = dataStore.AsMemory(iterationIndex * noOfYears, noOfYears);
                    return RunOneIteration(_objectives, iterationIndex, mySlice);
                })
                .ToList();

            // Sort the iteration results worst to best.
            var iterationResultsWorstToBest = iterationResults
                .OrderBy(iter => iter.SurvivedYears)
                .ThenBy(iter => iter.EndingBalance)
                .ToList()
                .AsReadOnly();

            return new SimResult(
                _objectives,
                iterationResultsWorstToBest
            );
        }

        private SimIteration RunOneIteration(IReadOnlyList<ISimObjective> objectives, int iterationIndex, Memory<SimYear> myWorstCaseSlice)
        {
            // Rent a context from the pool.
            using var lease = SimContextPool.Rent();
            var ctx = lease.Instance;

            // Reset the context for this iteration.
            ctx.Reset(InitPortfolio, iterationIndex, MySimParams.StartAge, myWorstCaseSlice);

            // Strategies can be stateful; Create new set of strategies for each iteration.
            var strategies = objectives.Select(x => x.CreateStrategy(iterationIndex)).ToList();

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
