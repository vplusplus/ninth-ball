
namespace NinthBall.Core
{
    internal sealed class SimBuilder(IEnumerable<ISimObjective> allObjectives)
    {
        private readonly Lazy<IReadOnlyList<ISimObjective>> LazyObjectives = new( () => CreateObjectivesOnce(allObjectives) );

        /// <summary>
        /// Immutable list of simulation objectives, sorted by execution order, prepared once.
        /// </summary>
        public IReadOnlyList<ISimObjective> SimulationObjectives => LazyObjectives.Value;

        private static IReadOnlyList<ISimObjective> CreateObjectivesOnce(IEnumerable<ISimObjective> allObjectives)
        {
            // Sort by preferred execution order suggested by each strategy
            return allObjectives.OrderBy(x => x.Order).ToList();
        }
    }
}
