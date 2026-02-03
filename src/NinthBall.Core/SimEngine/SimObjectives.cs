using System.Reflection;

namespace NinthBall.Core
{
    internal readonly record struct SimObjectiveInfo(Type Type, StrategyFamily Family);

    internal static class SimObjectives
    {
        public static IReadOnlyDictionary<string, SimObjectiveInfo> AllObjectives => LazyAllObjectives.Value;

        static readonly Lazy<IReadOnlyDictionary<string, SimObjectiveInfo>> LazyAllObjectives = new(() =>
        {
            var allObjectives = typeof(Simulation).Assembly
                .GetTypes()
                .Where(t => typeof(ISimObjective).IsAssignableFrom(t))      // Implements ISimObjective
                .Where(t => t != typeof(ISimObjective))                     // Is not ISimObjective itself
                .Where(t => !t.IsInterface)                                 // Is not just another interface
                .Where(t => !t.IsAbstract)                                  // We can create instances
                ;

            var map = new Dictionary<string, SimObjectiveInfo>(StringComparer.OrdinalIgnoreCase);

            foreach (var type in allObjectives)
            {
                var attr = type.GetCustomAttribute<StrategyFamilyAttribute>(inherit: false) ?? throw new Exception($"Fix the code | {type.FullName} has not declared its StrategyFamily.");
                map[type.Name] = new SimObjectiveInfo(type, attr.Family);
            }

            // Lock it down.
            return map.AsReadOnly();
        });

    }
}
