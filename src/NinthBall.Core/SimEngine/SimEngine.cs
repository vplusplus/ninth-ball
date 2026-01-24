
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
                .AddEmbeddedDefaultYamls()
                .AddYamlFile(simInputFileName)
                .Build();

            // Register components for the simulation.
            // NOTE: Dispose te DI container on return; we will prepare a fresh container for each run.
            simSessionBuilder.Services

                .RegisterConfigSection<SimulationSeed>()
                .RegisterConfigSection<SimParams>()
                .RegisterConfigSection<InitialBalance>()
                .RegisterConfigSection<TaxRateSchedules>()
                .RegisterConfigSection<MovingBlockBootstrapOptions>()
                .RegisterConfigSection<ParametricBootstrapOptions>()
                .RegisterConfigSection<Rebalance>()
                .RegisterConfigSection<AdditionalIncomes>()
                .RegisterConfigSection<LivingExpenses>()
                .RegisterConfigSection<FeesPCT>()
                .RegisterConfigSection<FlatTax>()
                .RegisterConfigSection<FixedWithdrawal>()
                .RegisterConfigSection<VariableWithdrawal>()
                .RegisterConfigSection<FlatGrowth>()

                .AddSingleton<HistoricalReturns>()
                .AddSingleton<FlatBootstrapper>()
                .AddSingleton<SequentialBootstrapper>()
                .AddSingleton<MovingBlockBootstrapper>()
                .AddSingleton<ParametricBootstrapper>()


                .AddSimulationStrategies()
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


        static IConfigurationBuilder AddEmbeddedDefaultYamls(this IConfigurationBuilder builder)
        {
            var simAssembly = typeof(SimEngine).Assembly;

            var embeddedDefaultsResourceNames = simAssembly
                .GetManifestResourceNames()
                .Where(name => null != name && name.Contains(".SimDefaults.") && name.EndsWith(".yaml"))
                .ToList();

            foreach(var resourceName in embeddedDefaultsResourceNames) builder.AddYamlResource(simAssembly, resourceName);

            return builder;
        }

        static IServiceCollection AddSimulationStrategies(this IServiceCollection services) => SimObjectivesSelector.AddSimulationObjectives(services);

    }
}
