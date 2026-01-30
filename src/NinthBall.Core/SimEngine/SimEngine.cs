
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace NinthBall.Core
{
    /// <summary>
    /// Runs one simulation.
    /// </summary>
    public static class SimEngine
    {
        public static async Task<SimResult> RunAsync(string simInputFileName)
        {
            ArgumentNullException.ThrowIfNull(simInputFileName);

            var simSessionBuilder = Host.CreateEmptyApplicationBuilder(settings: new());

            simSessionBuilder.Configuration
                .AddSimDefaults()
                .AddYamlFile(simInputFileName)
                .Build();

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
                .RegisterConfigSection<TaxAndMarketAssumptions>()

                .AddSingleton<ITaxSystem, SamAndHisBrothers>()
                .AddKeyedSingleton<ITaxGuesstimator, FederalTaxGuesstimator>(TaxAuthority.Federal)
                .AddKeyedSingleton<ITaxGuesstimator, NJTaxGuesstimator>(TaxAuthority.State)

                .RegisterConfigSection<FlatGrowth>()

                .AddSingleton<HistoricalReturns>()
                .AddSingleton<HistoricalBlocks>()
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
            foreach (var objInfo in SimObjectives.AllObjectives.Values) services.AddSingleton(objInfo.Type);
            return services;
        }
    }
}
