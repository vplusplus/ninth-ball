
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NinthBall.Core;
using NinthBall.Reports;
using NinthBall.Reports.Excel;
using NinthBall.Reports.Html;

namespace NinthBall.Reports
{
    public static class SimulationReportsExtensions
    {
        public static IHostApplicationBuilder ComposeReports(this IHostApplicationBuilder builder, string simOutputConfigFileName)
        {
            builder.Configuration
                .AddSimOutputConfigurations(simOutputConfigFileName)
                ;

            builder.Services
                .RegisterSimOutputOptions()
                .AddSimOutputComponents()
                .AddSingleton<ISimulationReports, SimulationReports>()
                ;

            return builder;
        }

        static IConfigurationBuilder AddSimOutputConfigurations(this IConfigurationBuilder builder, string simOutputConfigFileName)
        {
            var myAssembly = typeof(SimulationReportsExtensions).Assembly;

            var simOutputDefaultsResourceNames = myAssembly
                .GetManifestResourceNames()
                .Where(name => null != name)
                .Where(name => name.Contains(".OutputDefaults.", StringComparison.OrdinalIgnoreCase))
                .Where(name => name.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var res in simOutputDefaultsResourceNames) builder.AddYamlResource(myAssembly, res);

            builder.AddYamlFile(simOutputConfigFileName);

            return builder;
        }
    
        static IServiceCollection RegisterSimOutputOptions(this IServiceCollection services)
        {
            return services
                .RegisterConfigSection<OutputDefaults>()
                .RegisterConfigSection<OutputOptions>("Outputs")
                ;
        }

        static IServiceCollection AddSimOutputComponents(this IServiceCollection services)
        {
            return services
                .AddSingleton<OutputViews>()
                .AddSingleton<HtmlReport>()
                .AddSingleton<ExcelReport>()
                .AddSingleton<SimulationReports>()
                ;
        }

    }
}
