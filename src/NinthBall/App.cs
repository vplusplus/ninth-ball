
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NinthBall.Core;
using NinthBall.Outputs;
using NinthBall.Utils;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace NinthBall
{
    static class App
    {
        static readonly TimeSpan TwoSeconds  = TimeSpan.FromSeconds(2);
        static readonly TimeSpan FiveSeconds = TimeSpan.FromSeconds(5);
        static readonly TimeSpan TenMinutes  = TimeSpan.FromMinutes(10);

        static string InputConfigFileName  => Path.GetFullPath(CmdLine.Required("In"));
        static string OutputConfigFileName => Path.GetFullPath(CmdLine.Required("out"));
        static bool WatchMode => CmdLine.Switch("watch");
        static bool PrintHelp => CmdLine.Switch("help");

        public static async Task RunAsync()
        {
            if (PrintHelp) Print.Help();
                else if (WatchMode) await ProcessForever(); 
                    else await ProcessOnce();
        }

        static async Task ProcessForever()
        {
            var fileSet = new WatchFileSet(InputConfigFileName, OutputConfigFileName);
            var elapsed = Stopwatch.StartNew();

            while (elapsed.Elapsed < TenMinutes)
            {
                try
                {
                    // Check if input files had changed
                    if (fileSet.CheckForChangesAndRememberTimestamp())
                    {
                        // Reset the inactivity window
                        elapsed.Restart();

                        // Process once.
                        await ProcessOnce();
                    }

                    // Wait for some time...
                    await Task.Delay(TwoSeconds).ConfigureAwait(false);
                }
                catch (Exception warning) when (warning is FatalWarning or ValidationException)
                {
                    // Inform. Wait a bit longer
                    Console.WriteLine(warning.Message);
                    await Task.Delay(FiveSeconds).ConfigureAwait(false);
                }
                catch (Exception unhandledException)
                {
                    // Inform. Wait a bit longer
                    Print.ErrorSummaryAndDetails(unhandledException);
                    await Task.Delay(FiveSeconds).ConfigureAwait(false);
                }
            }

            // We stopped because of no activity.
            if (elapsed.Elapsed >= TenMinutes)
            {
                Console.WriteLine($" No changes detected for {TenMinutes.TotalMinutes:#,0} minutes. Looks like you forgot to stop me.");
                Console.WriteLine($" Sorry... STOPPING.");
            }
        }

        static async Task ProcessOnce()
        {
            // WHY?
            // Input and output configurations can change between runs.
            // We create and destroy DI container for each run of output generation.

            var simOutputSessionBuilder = Host.CreateEmptyApplicationBuilder(settings: new());

            simOutputSessionBuilder
                .ComposeSimulationSession(InputConfigFileName)
                .ComposeSimOutputSession(OutputConfigFileName)
                ;

            using (var session = simOutputSessionBuilder.Build())
            {
                // Run simulation
                var timer = Stopwatch.StartNew();
                var simResults = session.Services.GetRequiredService<ISimulation>().Run();
                timer.Stop();
                Print.Milestone("Simulation complete", timer.Elapsed);

                // Generate reports
                timer.Restart();
                await session.Services.GetRequiredService<ISimulationReports>().GenerateAsync(simResults);
                timer.Stop();
                Print.Milestone("Reports ready", timer.Elapsed);

                var sr = simResults.SurvivalRate;
                var txtSurvivalRate = sr > 0.99 ? $"{sr:P1}" : $"{sr:P0}";
                Console.WriteLine($" Survival rate: {txtSurvivalRate}");

            }
        }
    }
}

