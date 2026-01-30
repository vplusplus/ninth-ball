
using NinthBall.Core;
using NinthBall.Outputs.Excel;
using NinthBall.Outputs.Html;

namespace NinthBall.Outputs
{
    internal sealed class SimReports(HtmlOutputBuilder HtmlOutput, ExcelOutputBuilder ExcelOutput)
    {
        public async Task Generate(SimResult simResult)
        {
            try
            {
                await Task.WhenAll
                (
                    HtmlOutput.GenerateAsync(simResult), 
                    ExcelOutput.GenerateAsync(simResult)
                );
            }
            catch (Exception err)
            {
                var errHtmlFileName = Path.GetFullPath("./SimError.html");
                await HtmlOutput.GenerateErrorHtmlAsync(err, errHtmlFileName);

                throw new FatalWarning($"Report generation failed | See {errHtmlFileName}");
            }
        }
    }
}
