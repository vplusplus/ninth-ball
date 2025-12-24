
using System.Collections.ObjectModel;
using System.Reflection;

namespace NinthBall.Core
{
    /// <summary>
    /// Cache of metadata of the strategy implementations, discovered once using reflection.
    /// </summary>
    internal static class SimObjectivesCache
    {
        /// <summary>
        /// Describes a ISimObjective
        /// </summary>
        public readonly record struct MetaData(Type StrategyType, StrategyFamily Family, PropertyInfo? SimInputProperty);

        /// <summary>
        /// Provideds meta-data of ISimObjective implementations
        /// </summary>
        public static IReadOnlyCollection<MetaData> StrategyMetaData => Cache.Value;

        // Available implementations of ISimObjective, discovered once.
        private static readonly Lazy<ReadOnlyCollection<MetaData>> Cache = new(() =>
        {
            // Discover ISimObjective implementations
            var strategyImplementations = typeof(Simulation).Assembly
                .GetTypes()
                .Where(t => typeof(ISimObjective).IsAssignableFrom(t))      // Implements ISimObjective
                .Where(t => t != typeof(ISimObjective))                     // Is not ISimObjective itself
                .Where(t => !t.IsInterface)                                 // Is not just another interface
                .Where(t => !t.IsAbstract)                                  // We can create instances
                ;

            var metadata = new List<MetaData>();

            foreach (var type in strategyImplementations)
            {
                // Optional meta-data attribute
                var attr = type.GetCustomAttribute<SimInputAttribute>(inherit: false);

                // Strategy can optionaly declare its family.
                StrategyFamily family = null != attr ? attr.Family : StrategyFamily.None;

                // Strategy can optionaly declare the simulation input type (hence the SimInput property it wants)
                var inputProperty = null != attr && null != attr.OptionsType 
                    ? typeof(SimInput).GetProperties().FirstOrDefault(p => p.PropertyType == attr.OptionsType) 
                    : null;

                // Remember the strategy type, its family identifier and optional SimInput property
                metadata.Add(new(type, family, inputProperty));
            }

            // Lock it down.
            return metadata.AsReadOnly();
        });
    }
}
