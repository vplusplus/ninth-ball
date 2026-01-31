
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace NinthBall.Core
{
    public static class SimulationExtensions
    {
        public static IHostApplicationBuilder ComposeSimulation(this IHostApplicationBuilder builder, string simInputConfigFileName)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(simInputConfigFileName);

            builder.Configuration
                .AddYamlResources(typeof(Simulation).Assembly, ".SimDefaults.")
                .AddYamlFile(simInputConfigFileName);

            builder.Services
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

                .AddSingleton<ITaxSystem, SamAndHisBrothers>()
                .AddKeyedSingleton<ITaxGuesstimator, FederalTaxGuesstimator>(TaxAuthority.Federal)
                .AddKeyedSingleton<ITaxGuesstimator, NJTaxGuesstimator>(TaxAuthority.State)
                .AddSingleton<HistoricalReturns>()
                .AddSingleton<HistoricalBlocks>()
                .AddSingleton<FlatBootstrapper>()
                .AddSingleton<SequentialBootstrapper>()
                .AddSingleton<MovingBlockBootstrapper>()
                .AddSingleton<ParametricBootstrapper>()
                .AddSingleton<SimObjectivesSelector>()
                .AddSimulationObjectives()
                .AddSingleton<ISimulation, Simulation>()
                ;

            return builder;
        }

        static IServiceCollection AddSimulationObjectives(this IServiceCollection services)
        {
            foreach (var objInfo in SimObjectives.AllObjectives.Values) services.AddSingleton(objInfo.Type);
            return services;
        }
    }
}
