
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

            var simSessionBuilder = Host.CreateEmptyApplicationBuilder(settings: new());

            simSessionBuilder.Configuration
                .AddSimDefaults()
                .AddYamlFile(simInputFileName)
                .Build();

            // Register components for the simulation.
            // NOTE: Dispose the DI container on return; we will prepare a fresh container for each run.
            simSessionBuilder.Services

                .RegisterConfigSection<SimulationSeed>()
                .RegisterConfigSection<SimParams>()

                .RegisterConfigSection<Initial>()
                .RegisterConfigSection<YearlyRebalance>()
                .RegisterConfigSection<AdditionalIncomes>()
                .RegisterConfigSection<LivingExpenses>()

                .RegisterConfigSection<FixedWithdrawal>()
                .RegisterConfigSection<VariableWithdrawal>()

                .RegisterConfigSection<AnnualFees>()
                .RegisterConfigSection<FlatTax>()
                .RegisterConfigSection<TaxRateSchedules>()

                .RegisterConfigSection<FlatGrowth>()

                .AddSingleton<HistoricalReturns>()
                .AddSingleton<FlatBootstrapper>()
                .AddSingleton<SequentialBootstrapper>()
                .AddSingleton<MovingBlockBootstrapper>()
                .AddSingleton<ParametricBootstrapper>()
                .RegisterConfigSection<MovingBlockBootstrapOptions>()
                .RegisterConfigSection<ParametricBootstrapOptions>()

                .AddSimulationObjectives()
                .AddSingleton<SimObjectivesSelector>()
                .AddSingleton<Simulation>()

                .BuildServiceProvider();

            using (var simSession = simSessionBuilder.Build())
            {
                return simSession
                    .Services
                    .GetRequiredService<Simulation>()
                    .RunSimulation();
            }
        }

        static IConfigurationBuilder AddSimDefaults(this IConfigurationBuilder builder)
        {
            var simAssembly = typeof(SimEngine).Assembly;

            var simDefaultsResourceNames = simAssembly
                .GetManifestResourceNames()
                .Where(name => null != name)
                .Where(name => name.Contains(".SimDefaults.", StringComparison.OrdinalIgnoreCase))
                .Where(name => name.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach(var res in simDefaultsResourceNames) builder.AddYamlResource(simAssembly, res);

            return builder;
        }

        static IServiceCollection AddSimulationObjectives(this IServiceCollection services)
        {
            foreach ( var (friendlyName, objectiveInfo) in SimObjectivesSelector.KnownObjectives.Value) services.AddSingleton(objectiveInfo.Type);
            return services;
        }
    }
}
