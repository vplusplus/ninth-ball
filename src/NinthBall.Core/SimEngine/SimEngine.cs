
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        public static SimResult Run(string simInputFileName)
        {
            ArgumentNullException.ThrowIfNull(simInputFileName);

            // Deconstruct SimInput components, ignore nulls and validate if specified.
            var simInput = SimInputReader.ReadFromYamlFile(simInputFileName);
            var validInputs = simInput.DeconstructAndValidateSimulationInputs();

            var simSessionBuilder = Host.CreateEmptyApplicationBuilder(settings: new());

            simSessionBuilder.Configuration
                .AddYamlResource(typeof(SimEngine).Assembly, "SimulationOptions-Defaults.yaml")
                .AddYamlFile(simInputFileName)
                .Build();

            // Register components for the simulation.
            // NOTE: Dispose te DI container on return; we will prepare a fresh container for each run.
            simSessionBuilder.Services
                //.AddSingleton(new SimulationSeed(simInput.RandomSeedHint))
                .RegisterConfigSection<SimulationSeed>()

                .AddSingleton(simInput)
                .RegisterSimulationInputs(validInputs)
                .AddSimObjectives()
                .AddSingleton<SimObjectivesSelector>()
                .RegisterHistoricalDataAndBootstrappers()
                .RegisterTaxSchedules()
                .AddSingleton<Simulation>()
                .BuildServiceProvider();

            using (var simSession = simSessionBuilder.Build())
            {
                return simSession
                    .Services
                    .GetRequiredService<Simulation>()
                    .RunSimulation();
            }

            //// Run
            //return services
            //    .GetRequiredService<Simulation>()
            //    .RunSimulation();
        }


        static IServiceCollection AddSimObjectives(this IServiceCollection services) => SimObjectivesSelector.AddSimulationObjectives(services);


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

        // Register historical ROI data provider, bootstrappers and tax schedules.
        private static IServiceCollection RegisterHistoricalDataAndBootstrappers(this IServiceCollection services)
        {
            return services

                // Shared dependency that serves historical data to bootstrappers.
                .AddSingleton<HistoricalReturns>()

                // Add bootstrapper implementations and bootstrap selector
                .AddSingleton<FlatBootstrapper>()
                .AddSingleton<SequentialBootstrapper>()
                .AddSingleton<MovingBlockBootstrapper>()
                .AddSingleton<ParametricBootstrapper>()
                .AddSingleton<BootstrapSelector>()

                // Bootstrapper options - Optional configurations
                .AddSingleton((sp) => BootstrapConfiguration.GetMovingBlockBootstrapOptions())
                .AddSingleton((sp) => BootstrapConfiguration.GetParametricBootstrapOptions())
                
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
