using NinthBall.Core;
using NinthBall.Utils;
using NinthBall.Outputs.Html.Templates;

namespace NinthBall.Outputs.Html
{
    internal static class HtmlOutput
    {
        public static async Task GenerateAsync(IServiceProvider services, SimResult simResult, string inputFileName, string outputFileName, OutputOptions? outputConfig)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(simResult);
            ArgumentNullException.ThrowIfNull(outputFileName);

            // Prepare model, and render html
            Dictionary<string, object?> templateParameters = new() 
            { 
                [nameof(SimReport.InputFileName)] = inputFileName,
                [nameof(SimReport.SimResult)] = simResult,
                //[nameof(SimReport.OutputConfig)] = outputConfig,
            };
            var html = await HtmlTemplates.RenderTemplateAsync<SimReport>(services, templateParameters).ConfigureAwait(false);

            // Save
            FileSystem.EnsureDirectoryForFile(outputFileName);
            await File.WriteAllTextAsync(outputFileName, html);
        }

        public static async Task GenerateErrorHtmlAsync(IServiceProvider services, Exception err, string outputFileName)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(outputFileName);

            err = err ?? new Exception("Sorry, error object itself was null.");

            // Prepare model, and render html
            Dictionary<string, object?> templateParameters = new() { [nameof(SimErrors.Ex)] = err };
            var html = await HtmlTemplates.RenderTemplateAsync<SimErrors>(services, templateParameters).ConfigureAwait(false);

            // Save
            FileSystem.EnsureDirectoryForFile(outputFileName);
            await File.WriteAllTextAsync(outputFileName, html);
        }
    }
}
