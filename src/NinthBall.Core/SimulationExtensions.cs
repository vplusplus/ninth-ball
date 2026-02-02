
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace NinthBall.Core
{
    public static class SimulationExtensions
    {
        public static IConfigurationBuilder AddSimulationDefaults(this IConfigurationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            return builder.AddYamlResources(typeof(Simulation).Assembly, ".SimDefaults.");
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
                .RegisterConfigSection<MovingBlockBootstrapOptions>()
                .RegisterConfigSection<ParametricProfiles>()

                .AddSingleton<ITaxSystem, SamAndHisBrothers>()
                .AddKeyedSingleton<ITaxAuthority, FederalTaxGuesstimator>(TaxAuthority.Federal)
                .AddKeyedSingleton<ITaxAuthority, NJTaxGuesstimator>(TaxAuthority.State)
                .AddSingleton<HistoricalReturns>()
                .AddSingleton<HistoricalBlocks>()
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
