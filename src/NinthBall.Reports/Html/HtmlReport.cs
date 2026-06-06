using NinthBall.Core;
using NinthBall.Reports.Html.Templates;
using NinthBall.Utils;

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
            htmlFileName = PlaceHolders.ResolvePlaceholders(htmlFileName, GetTagResolver(simResult));
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


        static Func<string, string?, string> GetTagResolver(SimResult simResult)
        {
            return (string tag, string? format) =>
            {
                switch ((tag ?? string.Empty).ToLower())
                {
                    case "date":    return DateTime.Now.ToString(format ?? "yyyyMMdd");
                    case "initial": return $"{simResult.I0Y0.Jan.Total/1000000:F1}M";
                    case "y0exp":   return $"{simResult.I0Y0.Expenses.LivExp / 1000:F0}K";
                    case "y0age":   return $"{simResult.I0Y0.Age}Y";
                    case "growth":  return simResult.SimParams.Objectives.Where(x => x.Contains("Growth", StringComparison.OrdinalIgnoreCase)).FirstOrDefault() ?? string.Empty;
                    default: return null!;
                }
            };
        }

    }
}
