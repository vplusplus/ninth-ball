using NinthBall.Core;
using NinthBall.Utils;
using NinthBall.Reports.Html.Templates;

namespace NinthBall.Reports.Html
{
    internal sealed class HtmlReport(IServiceProvider Services, OutputDefaults Defaults, OutputViews Views, OutputOptions Options)
    {
        public async Task GenerateAsync(SimResult simResult)
        {
            ArgumentNullException.ThrowIfNull(simResult);

            var columns     = Views.ResolveView(Options.Html.View);
            var percentiles = Options.Html.Percentiles ?? Defaults.Percentiles ?? new double[0];
            var iterations  = Options.Html.Iterations ?? new int[0];

            // Prepare model, and render html
            Dictionary<string, object?> templateParameters = new() 
            { 
                [nameof(SimReport.SimResult)] = simResult,
                [nameof(SimReport.Columns)] = columns,
                [nameof(SimReport.Percentiles)] = percentiles,
                [nameof(SimReport.Iterations)] = iterations,
                [nameof(SimReport.TargetPercentile)] = Defaults.TargetPercentile,
            };
            var html = await HtmlTemplates.RenderTemplateAsync<SimReport>(Services, templateParameters).ConfigureAwait(false);

            // Save
            var htmlFileName = Path.GetFullPath(Options.Html.File);
            FileSystem.EnsureDirectoryForFile(htmlFileName);
            await File.WriteAllTextAsync(htmlFileName, html);

            Print.See("Html report", htmlFileName);
        }

        public async Task GenerateErrorHtmlAsync(Exception err, string errorHtmlFileName)
        {
            ArgumentNullException.ThrowIfNull(Services);

            err = err ?? new Exception("Sorry, error object itself was null.");

            // Prepare model, and render html
            Dictionary<string, object?> templateParameters = new() { [nameof(SimErrors.Ex)] = err };
            var html = await HtmlTemplates.RenderTemplateAsync<SimErrors>(Services, templateParameters).ConfigureAwait(false);

            // Save
            FileSystem.EnsureDirectoryForFile(errorHtmlFileName);
            await File.WriteAllTextAsync(errorHtmlFileName, html);
        }
    }
}
