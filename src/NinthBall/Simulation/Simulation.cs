
namespace NinthBall
{
    sealed class Simulation(SimParams MySimParams, InitialBalance InitPortfolio, SimBuilder MySimBuilder, GrowthStrategy GrowthStrategy)
    {
        public SimResult RunSimulation()
        {
            // Create simulation objectives.
            var objectives = MySimBuilder.SimulationObjectives;

            // TODO: Consult current bootstrapper. Find max iterations that we can support.
            int maxIterations = Math.Min(MySimParams.Iterations, GrowthStrategy.MaxIterations);

            // RunAsync iterations; Collect results.
            var iterationResults = Enumerable.Range(0, maxIterations)
                .Select(iterationIndex => RunOneIteration(objectives, iterationIndex))
                // .AsParallel()
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
    
        private SimIteration RunOneIteration(IReadOnlyList<ISimObjective> objectives, int iterationIndex)
        {
            // SimContext that tracks running balance 
            var ctx = new SimContext(InitPortfolio, IterationIndex: iterationIndex, StartAge: MySimParams.StartAge);

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
