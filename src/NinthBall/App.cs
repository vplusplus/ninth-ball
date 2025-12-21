
using Microsoft.Extensions.Configuration;
using NinthBall.Hosting;
using NinthBall.Templates;
using System.Diagnostics;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("UnitTests")]

namespace NinthBall
{
    internal sealed class App(IConfiguration config, Simulation simRunner)
    {
        static readonly TimeSpan TwoSeconds  = TimeSpan.FromSeconds(2);
        static readonly TimeSpan FiveSeconds = TimeSpan.FromSeconds(5);
        static readonly TimeSpan TenMinutes  = TimeSpan.FromMinutes(10);

        string InputFileName  => config.GetValue<string>("In") ?? throw new Exception("Input file name not specified.");
        string OutputFileName => config.GetValue<string>("Output") ?? throw new Exception("Output file name not specified.");
        bool WatchMode => CmdLine.Switch("watch");

        public async Task RunAsync()
        {
            // Ensure output directory.
            var outputDir = Path.GetDirectoryName(OutputFileName) ?? "./";
            if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

            // Process.
            if (WatchMode) await ProcessForever(InputFileName, OutputFileName); else await ProcessOnce(InputFileName, OutputFileName);
        }

        async Task ProcessForever(string inputFileName, string outputFileName)
        {
            var fileSet = new WatchFileSet(inputFileName);

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
                        await ProcessOnce(inputFileName, outputFileName);
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

        async Task ProcessOnce(string inputFileName, string outputFileName)
        {
            Console.WriteLine();

            try
            {
                // RunAsync simulation
                var timer = Stopwatch.StartNew();
                var simResult = simRunner.RunSimulation();
                timer.Stop();

                // Generate html report
                var html = await MyTemplates.GenerateSimReportAsync(simResult).ConfigureAwait(false);
                File.WriteAllText(outputFileName, html);

                // Inform                
                Print.Done(simResult, timer.Elapsed, outputFileName);
            }
            catch (Exception err)
            {
                // Print essentials to console
                Print.ErrorSummary(err);

                // Capture details to output file.
                var html = await MyTemplates.GenerateErrorHtmlAsync(err).ConfigureAwait(false);
                File.WriteAllText(outputFileName, html);
            }
        }
    }
}
