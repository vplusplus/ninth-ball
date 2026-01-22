
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace NinthBall.Core
{
    /// <summary>
    /// The Monte Carlo simulation engine.
    /// Orchestrates the simulation process, runs one simulation.
    /// </summary>
    public static class SimEngine
    {
        public static SimResult Run(SimInput input)
        {
            // Deconstruct SimInput components, ignore nulls and validate if specified.
            var validInputs = input.DeconstructAndValidateSimulationInputs();

            // Register components for the simulation.
            // NOTE: Dispose te DI container on return; we will prepare a fresh container for each run.
            using var services = new ServiceCollection()
                .AddSingleton(new SimulationSeed(input.RandomSeedHint))
                .AddSingleton(input)
                .RegisterSimulationInputs(validInputs)
                .RegisterActiveStrategies(validInputs)
                .RegisterHistoricalDataAndBootstrapper()
                .RegisterTaxSchedules()
                .AddSingleton<Simulation>()
                .BuildServiceProvider();

            // Run
            return services
                .GetRequiredService<Simulation>()
                .RunSimulation();
        }

        // Discover SimInput properties that are not NULL.
        // Validate each active-input using DataAnnotations attributes.
        private static Dictionary<PropertyInfo, object> DeconstructAndValidateSimulationInputs(this SimInput simInput)
        {
            ArgumentNullException.ThrowIfNull(simInput);

            Dictionary<PropertyInfo, object> pairs = new(LazySimInputProperties.Value.Count);

            foreach (var prop in LazySimInputProperties.Value)
            {
                // Read the SimInput component. 
                var value = prop.GetValue(simInput);

                // Ignore nulls. If present, validate the input.
                if (null != value) pairs.Add(prop, ThrowIfInvalid(value));
            }

            return pairs;
        }

        // Strategies look for simulation inputs from DI container.
        // Register all available inputs.
        private static IServiceCollection RegisterSimulationInputs(this IServiceCollection services, Dictionary<PropertyInfo, object> availableInputs)
        {
            foreach (var pair in availableInputs) services.AddSingleton(pair.Value.GetType(), pair.Value);
            return services;
        }

        // Register only active strategies to the DI system.
        // Strategies are active if: 
        // - They had not declared required input.
        // - They have declared required input and the required input is available in the SimInput.
        private static IServiceCollection RegisterActiveStrategies(this IServiceCollection services, Dictionary<PropertyInfo, object> availableInputs)
        {
            // Keep only active strategies.
            var activeStrategies = SimObjectivesCache.StrategyMetaData
                .Where(x => null == x.SimInputProperty || availableInputs.ContainsKey(x.SimInputProperty))
                .ToList();

            // Ensure more than one strategy is not active with-in each exclusive families
            EnforceStrategyExclusivity(activeStrategies);

            // Register active strategies to the DI system
            foreach (var strategy in activeStrategies) services.AddSingleton(typeof(ISimObjective), strategy.StrategyType);

            return services;
        }

        // Register historical ROI data provider, bootstrappers and tax schedules.
        private static IServiceCollection RegisterHistoricalDataAndBootstrapper(this IServiceCollection services)
        {
            return services

                // Shared dependency that serves historical data to bootstrappers.
                .AddSingleton<HistoricalReturns>()

                // Bootstrapper options - Optional configurations
                .AddSingleton((sp) => BootstrapConfiguration.GetMovingBlockBootstrapOptions())
                .AddSingleton((sp) => BootstrapConfiguration.GetParametricBootstrapOptions())

                // Available bootstrappers
                .AddSingleton<FlatBootstrapper>()
                .AddSingleton<SequentialBootstrapper>()
                .AddSingleton<MovingBlockBootstrapper>()
                .AddSingleton<ParametricBootstrapper>()

                // Chosen bootstrapper based on Growth option.
                .AddSingleton<IBootstrapper>(sp =>
                {
                    // Look for Growth options to determine which bootstrapper to use.
                    var growthOptions = sp.GetRequiredService<Growth>();

                    // If growth not specified, the Growth strategy should have been muted.
                    if (null == growthOptions) throw new InvalidOperationException("Growth option not specified. Request for Bootstrapper is invalid.");

                    // Choose the bootstrapper based on Growth options.
                    return growthOptions.Bootstrapper switch
                    {
                        BootstrapKind.Flat        => sp.GetRequiredService<FlatBootstrapper>(),
                        BootstrapKind.Sequential  => sp.GetRequiredService<SequentialBootstrapper>(),
                        BootstrapKind.MovingBlock => sp.GetRequiredService<MovingBlockBootstrapper>(),
                        BootstrapKind.Parametric  => sp.GetRequiredService<ParametricBootstrapper>(),
                        _ => throw new NotSupportedException($"Unknown bootstrapper kind: {growthOptions.Bootstrapper}"),
                    };
                })
                ;
        }

        private static IServiceCollection RegisterTaxSchedules(this IServiceCollection services)
        {
            return services

                // Tax Schedules for DI injection
                .AddKeyedSingleton(TaxScheduleKind.Federal, (sp, key) => TaxRateSchedules.FromConfigOrDefault("Federal2026Joint", TaxRateSchedules.FallbackFed2026))
                .AddKeyedSingleton(TaxScheduleKind.LTCG, (sp, key) => TaxRateSchedules.FromConfigOrDefault("FederalLTCG2026Joint", TaxRateSchedules.FallbackFedLTCG2026))
                .AddKeyedSingleton(TaxScheduleKind.State, (sp, key) => TaxRateSchedules.FromConfigOrDefault("NJ2026Joint", TaxRateSchedules.FallbackNJ2026))
                ;
        }


        // For some strategy families, only one of its kind is allowed.
        // Throws an exception if more than one strategy is activated with-in each exclusive-families.
        private static void EnforceStrategyExclusivity(IList<SimObjectivesCache.MetaData> activeStrategies)
        {
            var exclusiveFamilies = new[]
            {
                StrategyFamily.LifestyleExpenses,
                StrategyFamily.PreTaxWithdrawalVelocity,
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
