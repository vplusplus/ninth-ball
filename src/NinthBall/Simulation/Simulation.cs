
namespace NinthBall
{
    sealed class Simulation(SimParams MySimParams, InitialBalance InitPortfolio, SimBuilder MySimBuilder, GrowthStrategy GrowthStrategy)
    {
        public SimResult RunSimulation()
        {
            // Create simulation objectives.
            var objectives = MySimBuilder.SimulationObjectives;

            // Consult current bootstrapper. Find max iterations that we can support.
            int maxIterations = Math.Min(MySimParams.Iterations, GrowthStrategy.MaxIterations);
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
                    return RunOneIteration(objectives, iterationIndex, mySlice);
                })
                .ToList();

            // Sort the iteration results worst to best.
            var iterationResultsWorstToBest = iterationResults
                .OrderBy(iter => iter.SurvivedYears)
                .ThenBy(iter => iter.EndingBalance)
                .ToList()
                .AsReadOnly();

            return new SimResult(
                objectives,
                iterationResultsWorstToBest
            );
        }
    
        private SimIteration RunOneIteration(IReadOnlyList<ISimObjective> objectives, int iterationIndex, Memory<SimYear> myWorstCaseSlice)
        {
            // SimContext that tracks running balance and writes to our slice.
            var ctx = new SimContext(InitPortfolio, iterationIndex, MySimParams.StartAge, myWorstCaseSlice);

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

                if (!success ) break;
            }

            return new(iterationIndex, success, ctx.PriorYears);
        }
    }
}
