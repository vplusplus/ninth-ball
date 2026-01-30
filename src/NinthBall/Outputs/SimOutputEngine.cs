using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NinthBall.Core;
using NinthBall.Outputs;
using NinthBall.Outputs.Excel;
using NinthBall.Outputs.Html;

namespace NinthBall.Outputs
{
    internal static class SimOutputEngine
    {
        public static async Task GenerateAsync(SimResult simResult, string simOutputConfigFileName)
        {
            ArgumentNullException.ThrowIfNull(simOutputConfigFileName);

            // WHY?
            // Output configurtio can change between runs.
            // We create and destroy DI container for each run of output generation.

            var simOutputSessionBuilder = Host.CreateEmptyApplicationBuilder(settings: new());

            simOutputSessionBuilder.Configuration
                .AddOutputDefaults()
                .AddYamlFile(simOutputConfigFileName)
                .Build();

            simOutputSessionBuilder.Services
                .RegisterConfigSection<OutputDefaults>()
                .RegisterConfigSection<OutputOptions>("Outputs")
                .RegisterConfigSection<IReadOnlyDictionary<string, IReadOnlyList<CID>>>("Views")
                .AddSingleton<ViewRegistry>()
                .AddSingleton<SimReports>()
                .AddSingleton<HtmlOutputBuilder>()
                .AddSingleton<ExcelOutputBuilder>()
                .BuildServiceProvider()
                ;

            using (var simOutputSessionHost = simOutputSessionBuilder.Build())
            {
                await simOutputSessionHost
                    .Services
                    .GetRequiredService<SimReports>()
                    .Generate(simResult);
            }
        }

        static IConfigurationBuilder AddOutputDefaults(this IConfigurationBuilder builder)
        {
            var myAssembly = typeof(SimOutputEngine).Assembly;

            var simOutputDefaultsResourceNames = myAssembly
                .GetManifestResourceNames()
                .Where(name => null != name)
                .Where(name => name.Contains(".OutputDefaults.", StringComparison.OrdinalIgnoreCase))
                .Where(name => name.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var res in simOutputDefaultsResourceNames) builder.AddYamlResource(myAssembly, res);

            return builder;
        }
    }
}
