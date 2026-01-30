using NinthBall.Core;
using NinthBall.Utils;
using NinthBall.Outputs.Html.Templates;

namespace NinthBall.Outputs.Html
{
    internal sealed class HtmlReport(IServiceProvider services, OutputDefaults Defaults, OutputViews Views, OutputOptions Options)
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
                [nameof(SimReport.InputFileName)] = CmdLine.Required("in"),     // Indicated in html
                [nameof(SimReport.SimResult)] = simResult,

                [nameof(SimReport.Columns)] = columns,
                [nameof(SimReport.Percentiles)] = percentiles,
                [nameof(SimReport.Iterations)] = iterations

            };
            var html = await HtmlTemplates.RenderTemplateAsync<SimReport>(services, templateParameters).ConfigureAwait(false);

            // Save
            var htmlFileName = Path.GetFullPath(Options.Html.File);
            FileSystem.EnsureDirectoryForFile(htmlFileName);
            await File.WriteAllTextAsync(htmlFileName, html);

            Print.See("Html report", htmlFileName);
        }

        public async Task GenerateErrorHtmlAsync(Exception err, string errorHtmlFileName)
        {
            ArgumentNullException.ThrowIfNull(services);

            err = err ?? new Exception("Sorry, error object itself was null.");

            // Prepare model, and render html
            Dictionary<string, object?> templateParameters = new() { [nameof(SimErrors.Ex)] = err };
            var html = await HtmlTemplates.RenderTemplateAsync<SimErrors>(services, templateParameters).ConfigureAwait(false);

            // Save
            FileSystem.EnsureDirectoryForFile(errorHtmlFileName);
            await File.WriteAllTextAsync(errorHtmlFileName, html);
        }
    }
}
