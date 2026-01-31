
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NinthBall.Core;
using NinthBall.Reports;
using NinthBall.Reports.Excel;
using NinthBall.Reports.Html;

namespace NinthBall.Reports
{
    public static class SimulationReportsExtensions
    {
        public static IConfigurationBuilder AddReportDefaults(this IConfigurationBuilder builder) => builder
            .AddYamlResources(typeof(SimulationReports).Assembly, ".ReportDefaults.");

        public static IServiceCollection AddReportComponents(this IServiceCollection services)
        {
            services
                .RegisterConfigSection<OutputDefaults>()
                .RegisterConfigSection<OutputOptions>("Outputs")
                .AddSingleton<OutputViews>()
                .AddSingleton<HtmlReport>()
                .AddSingleton<ExcelReport>()
                .AddSingleton<ISimulationReports, SimulationReports>();

            return services;
        }
    }
}
