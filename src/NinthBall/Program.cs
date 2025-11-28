using System.Diagnostics;
using NinthBall.Templates;

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
                var watch = CmdLine.Switch("watch");
                if (watch) await ProcessForEver(); else await ProcessOnce();
            }
            catch(FatalWarning warn)
            {
                Console.WriteLine(warn.Message);
            }
            catch (Exception err)
            {
                Print.Error(err);
            }
        }

        static async Task ProcessForEver()
        {
            TimeSpan TwoSeconds = TimeSpan.FromSeconds(2);
            TimeSpan FiveSeconds = TimeSpan.FromSeconds(5);

            var inputFileName = CmdLine.Required("in");
            var oldTimestamp = DateTime.MinValue;

            Console.WriteLine($" WATCHING: {Path.GetFullPath(inputFileName)}");
            Console.WriteLine();

            int errorCount = 0;
            var elapsed = Stopwatch.StartNew();

            while (true)
            {
                try
                {
                    if (elapsed.Elapsed > TimeSpan.FromHours(2))
                    {
                        Console.WriteLine(" Watching too long. Looks like you forgot to stop me.");
                        Console.WriteLine(" Sorry... STOPPING.");
                        return;
                    }

                    var newTimestamp = File.GetLastWriteTime(inputFileName);
                    if (newTimestamp != oldTimestamp)
                    {
                        await ProcessOnce();
                        oldTimestamp = newTimestamp;
                        errorCount = 0;
                    }

                    // Wait
                    await Task.Delay(TwoSeconds).ConfigureAwait(false);
                }
                catch
                {
                    if (++errorCount > 2) throw new FatalWarning("Too many errors. Stopped watching.");
                    await Task.Delay(FiveSeconds).ConfigureAwait(false);
                }
            }
        }

        static async Task ProcessOnce()
        {
            var inputFileName = CmdLine.Required("in");
            var outputFileName = "./Output.html";

            try
            {
                // Read Simulation configurations
                var simConfig = SimConfigReader.Read(inputFileName);

                // Capture output file name here before proceeding
                outputFileName = simConfig.Output;
                var outputDir = Path.GetDirectoryName(outputFileName) ?? "./";
                if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

                // Validate and print the params 
                simConfig.ThrowIfInvalid().PrintParams();

                // Run simulation
                var timer = Stopwatch.StartNew();
                var simResult = Simulation.RunSimulation(simConfig);
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
                Print.Error(err);
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
