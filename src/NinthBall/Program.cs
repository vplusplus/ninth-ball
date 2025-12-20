
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NinthBall.Hosting;
using NinthBall.Templates;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("UnitTests")]

namespace NinthBall
{
    internal static class Program
    {
        static async Task Main(string[] args)
        {
            Print.Header();

            try
            {
                await MyHost.DefineMyApp().Services.GetRequiredService<App>().RunAsync();
            }
            catch(FatalWarning warn)
            {
                Console.WriteLine(warn.Message);
            }
            catch (ValidationException validationErr)
            {
                Console.WriteLine(validationErr.Message);
            }
            catch (Exception err)
            {
                Print.Error(err);
            }
        }
    }
    
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
            if (WatchMode) await ProcessForever(); else await ProcessOnce();
        }

        async Task ProcessForever()
        {
            var inputFileName = Path.GetFullPath(InputFileName);
            var fileSet = new WatchFileSet(inputFileName);

            int errorCount = 0;
            var elapsed = Stopwatch.StartNew();

            while (true)
            {
                try
                {
                    // Stop waiting if input file not changed for more than 10 minutes.
                    if (elapsed.Elapsed > TenMinutes)
                    {
                        Console.WriteLine($" No changes detected for {TenMinutes.TotalMinutes:#,0} minutes. Looks like you forgot to stop me.");
                        Console.WriteLine(" Sorry... STOPPING.");
                        return;
                    }

                    // Check if input files had changed
                    bool somethingChanged = fileSet.CheckForChangesAndRememberTimestamp();
                    if (somethingChanged)
                    {
                        // File has changed, Process the file.
                        await ProcessOnce();
                        errorCount = 0;
                        elapsed.Restart();
                    }

                    // Wait for some time...
                    await Task.Delay(TwoSeconds).ConfigureAwait(false);
                }
                catch (FatalWarning)
                {
                    throw;
                }
                catch (Exception err)
                {
                    if (++errorCount > 2) throw new Exception("2-many errors. Stopped watching.", err);
                    await Task.Delay(FiveSeconds).ConfigureAwait(false);
                }
            }
        }

        async Task ProcessOnce()
        {
            var inputFileName = Path.GetFullPath(this.InputFileName);
            var outputFileName = Path.GetFullPath(this.OutputFileName);

            // Ensure output directory 
            var outputDir = Path.GetDirectoryName(outputFileName) ?? "./";
            if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

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
                Print.Footer(simResult, timer.Elapsed, outputFileName);
                Console.WriteLine();
            }
            catch (FatalWarning warn)
            {
                SaveErrorHtml(warn, outputFileName);
                Console.WriteLine($" FATAL WARNING: {warn.Message}");
                Console.WriteLine();
            }
            catch (Exception err)
            {
                SaveErrorHtml(err, outputFileName);
                throw;
            }
        }

        static void SaveErrorHtml(Exception err, string outputFileName)
        {
            var html = MyTemplates.GenerateErrorHtmlAsync(err).ConfigureAwait(false).GetAwaiter().GetResult();
            File.WriteAllText(outputFileName, html);
        }
    }

}
