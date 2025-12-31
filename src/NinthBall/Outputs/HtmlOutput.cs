using NinthBall.Core;
using NinthBall.Templates;

namespace NinthBall
{
    internal static class HtmlOutput
    {
        public static async Task Generate(SimResult simResult, string htmlFileName)
        {
            ArgumentNullException.ThrowIfNull(simResult);
            ArgumentNullException.ThrowIfNull(htmlFileName);

            // Generate
            Dictionary<string, object?> templateParameters = new()
            {
                ["Model"] = simResult
            };
            var html = await MyTemplates.GenerateSimReportAsync(simResult).ConfigureAwait(false);

            // Save
            FileSystem.EnsureDirectoryForFile(htmlFileName);
            File.WriteAllText(htmlFileName, html);
        }
    }
}
