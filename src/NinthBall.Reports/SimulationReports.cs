
using NinthBall.Core;
using NinthBall.Reports;
using NinthBall.Reports.Excel;
using NinthBall.Reports.Html;

namespace NinthBall.Reports
{
    public interface ISimulationReports
    {
        Task GenerateAsync(SimResult simResult);
    }

    internal sealed class SimulationReports(HtmlReport HtmlReport, ExcelReport ExcelReport) : ISimulationReports
    {
        public async Task GenerateAsync(SimResult simResult)
        {
            try
            {
                await Task.WhenAll
                (
                    HtmlReport.GenerateAsync(simResult),
                    ExcelReport.GenerateAsync(simResult)
                );
            }
            catch (Exception err)
            {
                var errHtmlFileName = Path.GetFullPath("./SimError.html");
                await HtmlReport.GenerateErrorHtmlAsync(err, errHtmlFileName);
                throw new FatalWarning($"Report generation failed | See {errHtmlFileName}");
            }
        }
    }
}
