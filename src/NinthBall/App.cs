
using NinthBall.Core;
using NinthBall.Templates;
using System.Diagnostics;

namespace NinthBall
{
    internal sealed class App(IServiceProvider Services)
    {
        static readonly TimeSpan TwoSeconds  = TimeSpan.FromSeconds(2);
        static readonly TimeSpan FiveSeconds = TimeSpan.FromSeconds(5);
        static readonly TimeSpan TenMinutes  = TimeSpan.FromMinutes(10);

        string InputFileName  => Path.GetFullPath(CmdLine.Required("In"));
        string OutputFileName => Path.GetFullPath(CmdLine.Optional("Out", Path.ChangeExtension(InputFileName, ".html") ));
        bool WatchMode => CmdLine.Switch("watch");

        public async Task RunAsync()
        {
            // Ensure output directory.
            var outputDir = Path.GetDirectoryName(OutputFileName) ?? "./";
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

            // Process
            if (WatchMode) await ProcessForever(); else await ProcessOnce();
        }

        async Task ProcessForever()
        {
            var fileSet = new WatchFileSet(InputFileName);

            int consecutiveErrorCount = 0;
            var elapsed = Stopwatch.StartNew();

            while (true)
            {
                try
                {
                    // Stop watching if input file not changed for more than 10 minutes.
                    if (elapsed.Elapsed > TenMinutes)
                    {
                        Console.WriteLine($" No changes detected for {TenMinutes.TotalMinutes:#,0} minutes. Looks like you forgot to stop me.");
                        Console.WriteLine($" Sorry... STOPPING.");
                        return;
                    }

                    // Check if input files had changed
                    bool somethingChanged = fileSet.CheckForChangesAndRememberTimestamp();
                    if (somethingChanged)
                    {
                        // File has changed, Process the file.
                        await ProcessOnce();
                        consecutiveErrorCount = 0;
                        elapsed.Restart();
                    }

                    // Wait for some time...
                    await Task.Delay(TwoSeconds).ConfigureAwait(false);
                }
                catch
                {
                    // If too many unhandled exceptions stop processing. 
                    // Or else, give some additional time and try again.
                    if (++consecutiveErrorCount > 5) return; else await Task.Delay(FiveSeconds).ConfigureAwait(false);
                }
            }
        }

        async Task ProcessOnce()
        {
            Console.WriteLine();

            try
            {
                // Load Config
                var simConfig = SimInputReader.ReadFromYamlFile(InputFileName);

                // Run simulation
                var timer = Stopwatch.StartNew();
                var simResult = SimEngine.Run(simConfig);
                timer.Stop();

                var htmlFileName = OutputFileName;
                await HtmlOutput.GenerateAsync(Services, simResult, InputFileName, htmlFileName);
                Console.WriteLine($" Html report  | See {htmlFileName}");

                try
                {
                    var excelFileName = Path.ChangeExtension(htmlFileName, ".xlsx");
                    await ExcelOutput.Generate(simResult, excelFileName);
                    Console.WriteLine($" Excel report | See {excelFileName}");
                }
                catch(System.IO.IOException ioErr)
                {
                    // Excel file is probably currently open.
                    Console.WriteLine(" WRNING: Excel report not generted, if present, may not agree with html report.");
                    Console.WriteLine($" {ioErr.Message}");
                }

                // Inform                
                Print.Done(simResult, timer.Elapsed);
            }
            catch (Exception err)
            {
                // Print essentials to console
                Print.ErrorSummary(err);

                // Capture details to output file.
                await HtmlOutput.GenerateErrorHtmlAsync(Services, err, OutputFileName).ConfigureAwait(false);
            }
        }
    }
}
