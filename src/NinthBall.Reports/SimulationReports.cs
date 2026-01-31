
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

    internal sealed class SimulationReports(HtmlReport HtmlReport, ExcelReport ExcelReport, OutputOptions Options) : ISimulationReports
    {
        // BY-DESIGN: Error is saved to html-report. Reason: User will be watching that file.
        string ErrorHtmlFileName => Options.Html.File ?? "./SimError.html";

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
                FileSystem.EnsureDirectoryForFile(ErrorHtmlFileName);
                await HtmlReport.GenerateErrorHtmlAsync(err, ErrorHtmlFileName);
                throw new FatalWarning($"Report generation failed | See {ErrorHtmlFileName}");
            }
        }
    }
}
