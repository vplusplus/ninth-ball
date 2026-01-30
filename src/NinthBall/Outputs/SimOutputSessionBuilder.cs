
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NinthBall.Core;
using NinthBall.Outputs;
using NinthBall.Outputs.Excel;
using NinthBall.Outputs.Html;

namespace NinthBall.Outputs
{
    internal static class SimOutputSessionBuilder
    {
        public static IHostApplicationBuilder ComposeSimOutputSession(this IHostApplicationBuilder simSessionBuilder, string simOutputConfigFileName)
        {
            simSessionBuilder.Configuration
                .AddSimOutputConfigurations(simOutputConfigFileName)
                ;

            simSessionBuilder.Services
                .RegisterSimOutputOptions()
                .AddSimOutputComponents()
                .AddSingleton<ISimulationReports, SimReports>()
                ;

            return simSessionBuilder;
        }

        static IConfigurationBuilder AddSimOutputConfigurations(this IConfigurationBuilder builder, string simOutputConfigFileName)
        {
            var myAssembly = typeof(SimOutputSessionBuilder).Assembly;

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
                .AddSingleton<SimReports>()
                ;
        }

    }
}
