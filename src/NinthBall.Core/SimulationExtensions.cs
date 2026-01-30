
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace NinthBall.Core
{
    public static class SimulationExtensions
    {
        public static IHostApplicationBuilder ComposeSimulationSession(this IHostApplicationBuilder simSessionBuilder, string simInputConfigFileName)
        {
            ArgumentNullException.ThrowIfNull(simSessionBuilder);
            ArgumentNullException.ThrowIfNull(simInputConfigFileName);

            simSessionBuilder.Configuration
                .AddSimulationConfigurations(simInputConfigFileName)
                ;

            simSessionBuilder.Services
                .RegisterSimulationOptions()
                .AddSimulationComponents()
                .AddSingleton<ISimulation, Simulation>()
                ;

            return simSessionBuilder;
        }

        static IConfigurationBuilder AddSimulationConfigurations(this IConfigurationBuilder builder, string simInputConfigFileName)
        {
            var simAssembly = typeof(Simulation).Assembly;

            var simDefaultsResourceNames = simAssembly
                .GetManifestResourceNames()
                .Where(name => null != name)
                .Where(name => name.Contains(".SimDefaults.", StringComparison.OrdinalIgnoreCase))
                .Where(name => name.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach(var res in simDefaultsResourceNames) builder.AddYamlResource(simAssembly, res);

            builder.AddYamlFile(simInputConfigFileName);

            return builder;
        }

        static IServiceCollection RegisterSimulationOptions(this IServiceCollection services)
        {
            return services

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
                .RegisterConfigSection<FlatGrowth>()
                .RegisterConfigSection<MovingBlockBootstrapOptions>()
                .RegisterConfigSection<ParametricBootstrapOptions>()
                ;
        }

        static IServiceCollection AddSimulationComponents(this IServiceCollection services)
        {
            return services
                .AddSingleton<ITaxSystem, SamAndHisBrothers>()
                .AddKeyedSingleton<ITaxGuesstimator, FederalTaxGuesstimator>(TaxAuthority.Federal)
                .AddKeyedSingleton<ITaxGuesstimator, NJTaxGuesstimator>(TaxAuthority.State)
                .AddSingleton<HistoricalReturns>()
                .AddSingleton<HistoricalBlocks>()
                .AddSingleton<FlatBootstrapper>()
                .AddSingleton<SequentialBootstrapper>()
                .AddSingleton<MovingBlockBootstrapper>()
                .AddSingleton<ParametricBootstrapper>()
                .AddSimulationObjectives()
                .AddSingleton<SimObjectivesSelector>()
                .AddSingleton<Simulation>()
                ;
        }

        static IServiceCollection AddSimulationObjectives(this IServiceCollection services)
        {
            foreach (var objInfo in SimObjectives.AllObjectives.Values) services.AddSingleton(objInfo.Type);
            return services;
        }
    }

}
