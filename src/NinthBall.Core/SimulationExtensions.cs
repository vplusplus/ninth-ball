

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NinthBall.Core
{
    public static class SimulationExtensions
    {
        public static IConfigurationBuilder AddSimulationDefaults(this IConfigurationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            return builder.AddYamlResources(typeof(Simulation).Assembly, ".SimDefaults.");
        }

        public static IConfigurationBuilder AddOverrides(this IConfigurationBuilder builder, SimInputOverrides overrides)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(overrides);

            return builder.AddInMemoryCollection(overrides);
        }

        public static IServiceCollection AddSimulationComponents(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services
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
                .RegisterConfigSection<BootstrapOptions>()
                .RegisterConfigSection<ParametricProfiles>()

                .AddSingleton<ITaxSystem, SamAndHisBrothers>()
                .AddKeyedSingleton<ITaxAuthority, FederalTaxGuesstimator>(TaxAuthority.Federal)
                .AddKeyedSingleton<ITaxAuthority, NJTaxGuesstimator>(TaxAuthority.State)
                .AddSingleton<HistoricalReturns>()
                .AddSingleton<HistoricalBlocks>()
                .AddSingleton<HistoricalRegimes>()
                .AddSingleton<SimObjectivesSelector>()
                .AddSimulationObjectives()
                .AddSingleton<ISimulation, Simulation>()
                ;

            return services;
        }

        static IServiceCollection AddSimulationObjectives(this IServiceCollection services)
        {
            foreach (var objInfo in SimObjectives.AllObjectives.Values) services.AddSingleton(objInfo.Type);
            return services;
        }
    }
}
