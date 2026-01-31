
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
                .AddYamlResources(typeof(SimulationReports).Assembly, ".OutputDefaults.")
                .AddYamlFile(simOutputConfigFileName)
                ;

            builder.Services
                .RegisterConfigSection<OutputDefaults>()
                .RegisterConfigSection<OutputOptions>("Outputs")
                .AddSingleton<OutputViews>()
                .AddSingleton<HtmlReport>()
                .AddSingleton<ExcelReport>()
                .AddSingleton<ISimulationReports, SimulationReports>();

            return builder;
        }
    }
}
