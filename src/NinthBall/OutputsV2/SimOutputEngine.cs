using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NinthBall.Core;
using NinthBall.Outputs;
using NinthBall.Outputs.Excel;
using NinthBall.Outputs.Html;

namespace NinthBall.OutputsV2
{
    internal static class SimOutputEngine
    {
        public static void Generate(SimResult simResult, string simOutputConfigFileName)
        {
            ArgumentNullException.ThrowIfNull(simOutputConfigFileName);

            var simOutputSessionBuilder = Host.CreateEmptyApplicationBuilder(settings: new());

            simOutputSessionBuilder.Configuration
                .AddSimOutputDefaults()
                .AddYamlFile(simOutputConfigFileName)
                .Build();

            simOutputSessionBuilder.Services
                .RegisterConfigSection<OutputOptions>()
                .RegisterConfigSection<SimOutputFiles>()
                .AddSingleton<SimOutputSession>()
                .BuildServiceProvider();

            // NOTE:
            // Output config can change between runs.
            // We create and destroy DI container for each run of output generation.
            using (var simSession = simOutputSessionBuilder.Build())
            {
                simSession
                    .Services
                    .GetRequiredService<SimOutputSession>()
                    .GenerateOutput(simResult);
            }
        }

        static IConfigurationBuilder AddSimOutputDefaults(this IConfigurationBuilder builder)
        {
            var myAssembly = typeof(SimOutputEngine).Assembly;

            var simOutputDefaultsResourceNames = myAssembly
                .GetManifestResourceNames()
                .Where(name => null != name)
                .Where(name => name.Contains(".SimOutputDefaults.", StringComparison.OrdinalIgnoreCase))
                .Where(name => name.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var res in simOutputDefaultsResourceNames) builder.AddYamlResource(myAssembly, res);

            return builder;
        }

    }

    internal sealed class SimOutputSession(IServiceProvider Services, SimOutputFiles OutputFileNames, OutputOptions outputConfig)
    {
        public void GenerateOutput(SimResult simResult)
        {
            var InputFileName = "STUBBED";

            try
            {
                var htmlFileName = OutputFileNames.HtmlFileName;
                HtmlOutput.GenerateAsync(Services, simResult, InputFileName, htmlFileName, outputConfig)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();

                Console.WriteLine($" Html report  | See {htmlFileName}");
            }
            catch (Exception err) 
            {
                HtmlOutput.GenerateErrorHtmlAsync(Services, err, OutputFileNames.HtmlFileName)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
            }

            try
            {
                var excelFileName = OutputFileNames.ExcelFileName;
                ExcelOutput.Generate(simResult, excelFileName, outputConfig)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();

                Console.WriteLine($" Excel report | See {excelFileName}");
            }
            catch (System.IO.IOException ioErr)
            {
                // Excel file is probably currently open.
                Console.WriteLine(" WARNING: Excel report not generated, if present, may not agree with html report.");
                Console.WriteLine($" {ioErr.Message}");
            }
        }
    }
}
