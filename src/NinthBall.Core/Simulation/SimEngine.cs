

using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace NinthBall.Core
{
    /// <summary>
    /// The Monte Carlo simulation engine. Orchestrates the entire simulation process.
    /// </summary>
    public static class SimEngine
    {
        public static SimResult Run(SimInput input)
        {
            // Deconstruct SimInput components, ignore nulls and validate if specified.
            var validInputs = input.DeconstructAndValidateSimulationInputs();

            // Register components for the simulation.
            // Note: Dispose on return.
            using var services = new ServiceCollection()
                .AddSingleton(new SimulationSeed(input.RandomSeedHint))
                .RegisterSimulationInputs(validInputs)
                .RegisterActiveStrategies(validInputs)
                .RegisterHistoricalReturnsAndBootstrappers()
                .AddSingleton<Simulation>()
                .BuildServiceProvider();

            // Run
            return services
                .GetRequiredService<Simulation>()
                .RunSimulation();
        }

        private static IServiceCollection RegisterHistoricalReturnsAndBootstrappers(this IServiceCollection services)
        {
            return services
                .AddSingleton<HistoricalReturns>()
                .AddSingleton<FlatBootstrapper>()
                .AddSingleton<SequentialBootstrapper>()
                .AddSingleton<MovingBlockBootstrapper>()
                .AddSingleton<ParametricBootstrapper>()
                ;
        }

        private static Dictionary<PropertyInfo, object> DeconstructAndValidateSimulationInputs(this SimInput simInput)
        {
            ArgumentNullException.ThrowIfNull(simInput);

            Dictionary<PropertyInfo, object> pairs = new();

            foreach (var prop in LazySimInputProperties.Value)
            {
                // Read the SimInput component. 
                var value = prop.GetValue(simInput);

                // Ignore nulls. If present, validate the input.
                if (null != value) pairs.Add(prop, ThrowIfInvalid(value));
            }

            return pairs;
        }

        private static IServiceCollection RegisterSimulationInputs(this IServiceCollection services, Dictionary<PropertyInfo, object> availableInputs)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(availableInputs);

            // Strategies look for simulation inputs from DI container.
            // Register all available inputs.
            foreach (var pair in availableInputs) services.AddSingleton(pair.Value.GetType(), pair.Value);
            return services;
        }

        private static IServiceCollection RegisterActiveStrategies(this IServiceCollection services, Dictionary<PropertyInfo, object> availableInputs)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(availableInputs);

            // Strategies are active if:
            // - They had not declared required input.
            // - They have declared required input and the required input is available in the SimInput.
            var activeStrategies = SimObjectivesCache.StrategyMetaData
                .Where(x => null == x.SimInputProperty || availableInputs.ContainsKey(x.SimInputProperty))
                .ToList();

            // Ensure more than one strategy is not active with-in each exclusive families
            EnforceStrategyExclusivity(activeStrategies);

            // Register active strategies to the DI system
            foreach(var strategy in activeStrategies) services.AddSingleton(typeof(ISimObjective), strategy.StrategyType);

            return services;
        }

        // For some strategy families, only one of its kind is allowed.
        // Throws an exceotion if more than one strategy is activated with-in each exclusive-families.
        private static void EnforceStrategyExclusivity(IList<SimObjectivesCache.MetaData> activeStrategies)
        {
            var exclusiveFamilies = new[]
            {
                StrategyFamily.LifestyleExpenses,
                StrategyFamily.WithdrawalVelocity,
                StrategyFamily.CashUsage,
                StrategyFamily.CashRefill,
                StrategyFamily.Taxes
            };

            foreach (var family in exclusiveFamilies)
            {
                var conflicting = activeStrategies.Where(s => s.Family == family).ToList();
                if (conflicting.Count > 1)
                {
                    var names = string.Join(", ", conflicting.Select(s => s.StrategyType.Name));
                    throw new FatalWarning($"Conflicting strategies detected in family '{family}' [{names}] | Only one strategy from this family can be active at a time.");
                }
            }
        }

        // Readable top level properties of the SimInput.
        // Complex types from the same namespace of SimInput.
        // Discovered using reflection ONCE.
        static readonly Lazy<ReadOnlyCollection<PropertyInfo>> LazySimInputProperties = new(() =>
        {
            var myNamespace = typeof(SimInput).Namespace;

            return typeof(SimInput)
                .GetProperties()
                .Where(p => p.CanRead)
                .Where(p => p.PropertyType.Namespace == myNamespace)
                .ToList()
                .AsReadOnly();
        });

        // Consult DataAnnotations attributes.
        // Throw if validation fails.
        private static object ThrowIfInvalid(object something)
        {
            var context = new ValidationContext(something, serviceProvider: null, items: null);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(something, context, results, validateAllProperties: true);

            if (!isValid)
            {
                var csvValidationErrors = string.Join(Environment.NewLine, results.Select(x => x.ErrorMessage));
                throw new ValidationException($"Validation failed for {something.GetType().Name}:{Environment.NewLine}{csvValidationErrors}");
            }

            return something;
        }
    }
}
