using NinthBall.Core;
using NinthBall.Templates;

namespace NinthBall
{
    internal static class HtmlOutput
    {
        public static async Task GenerateAsync(IServiceProvider services, SimResult simResult, string outputFileName)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(simResult);
            ArgumentNullException.ThrowIfNull(outputFileName);

            // Prepare model, and render html
            Dictionary<string, object?> templateParameters = new() { [nameof(Templates.SimReport.Model)] = simResult };
            var html = await HtmlTemplates.RenderTemplateAsync<Templates.SimReport>(services, templateParameters).ConfigureAwait(false);

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
            Dictionary<string, object?> templateParameters = new() { [nameof(Templates.ErrorDetails.Ex)] = err };
            var html = await HtmlTemplates.RenderTemplateAsync<Templates.ErrorDetails>(services, templateParameters).ConfigureAwait(false);

            // Save
            FileSystem.EnsureDirectoryForFile(outputFileName);
            await File.WriteAllTextAsync(outputFileName, html);
        }
    }
}
