
using Microsoft.Extensions.DependencyInjection;
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
            var services = new ServiceCollection();

            // 1. Register Data
            RegisterData(services, input);

            // 2. Register Engine Infrastructure
            RegisterEngine(services);

            // 3. Register Strategies (Discovery)
            RegisterStrategies(services, input);

            // 4. Build and Run
            using var provider = services.BuildServiceProvider();
            var simulation = provider.GetRequiredService<Simulation>();
            
            return simulation.RunSimulation();
        }

        private static void RegisterData(IServiceCollection services, SimInput config)
        {
            services.AddSingleton(config.SimParams);
            services.AddSingleton(config.InitialBalance);
            services.AddSingleton(new SimulationSeed(config.RandomSeedHint));

            if (config.ROIHistory != null) services.AddSingleton(Validate(config.ROIHistory));
            if (config.Growth != null) services.AddSingleton(Validate(config.Growth));
            if (config.FlatBootstrap != null) services.AddSingleton(Validate(config.FlatBootstrap));
            if (config.MovingBlockBootstrap != null) services.AddSingleton(Validate(config.MovingBlockBootstrap));
            if (config.ParametricBootstrap != null) services.AddSingleton(Validate(config.ParametricBootstrap));
        }

        private static void RegisterEngine(IServiceCollection services)
        {
            services.AddSingleton<HistoricalReturns>();
            services.AddSingleton<FlatBootstrapper>();
            services.AddSingleton<SequentialBootstrapper>();
            services.AddSingleton<MovingBlockBootstrapper>();
            services.AddSingleton<ParametricBootstrapper>();
            services.AddSingleton<Simulation>();
        }

        private static void RegisterStrategies(IServiceCollection services, SimInput config)
        {
            var strategyTypes = typeof(Simulation).Assembly.GetTypes()
                .Where(t => t.GetCustomAttributes<SimInputAttribute>(false).Any());

            var activeStrategies = new List<(SimInputAttribute Attr, Type StrategyType)>();

            foreach (var type in strategyTypes)
            {
                var attr = type.GetCustomAttribute<SimInputAttribute>(false)!;
                
                // Find the property in SimInput that matches the OptionsType
                var prop = typeof(SimInput).GetProperties()
                    .FirstOrDefault(p => p.PropertyType == attr.OptionsType);

                if (prop != null)
                {
                    var optionsValue = prop.GetValue(config);
                    if (optionsValue != null)
                    {
                        activeStrategies.Add((attr, type));
                        
                        // Register the options instance
                        services.AddSingleton(attr.OptionsType, Validate(optionsValue));
                        
                        // Register the strategy as ISimObjective
                        services.AddSingleton(typeof(ISimObjective), type);
                    }
                }
            }

            // Enforce exclusivity
            EnforceExclusivity(activeStrategies);
        }

        private static void EnforceExclusivity(List<(SimInputAttribute Attr, Type StrategyType)> activeStrategies)
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
                var conflicting = activeStrategies.Where(s => s.Attr.Family == family).ToList();
                if (conflicting.Count > 1)
                {
                    var names = string.Join(", ", conflicting.Select(s => s.StrategyType.Name));
                    throw new InvalidOperationException($"Conflicting strategies detected in family '{family}': {names}. Only one strategy from this family can be active at a time.");
                }
            }
        }

        private static T Validate<T>(T something) where T : class
        {
            var context = new ValidationContext(something, serviceProvider: null, items: null);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(something, context, results, validateAllProperties: true);
            
            if (!isValid)
            {
                var csvValidationErrors = string.Join(Environment.NewLine, results.Select(x => x.ErrorMessage));
                throw new ValidationException($"Validation failed for {typeof(T).Name}:{Environment.NewLine}{csvValidationErrors}");
            }
            
            return something;
        }

        private static object Validate(object something)
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
