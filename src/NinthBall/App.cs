
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NinthBall.Core;
using NinthBall.Reports;
using NinthBall.Utils;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace NinthBall
{
    static class App
    {
        static string InputConfigFileName  => CmdLine.Required("In");
        static string? OptionalOutputConfigFileName => CmdLine.Optional("out", null!);
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
            const int TenConsecutiveErrors = 10;

            TimeSpan TwoSeconds  = TimeSpan.FromSeconds(2);
            TimeSpan FiveSeconds = TimeSpan.FromSeconds(5);
            TimeSpan TenMinutes  = TimeSpan.FromMinutes(10);

            var fileSet  = new WatchFileSet(InputConfigFileName, OptionalOutputConfigFileName);
            var inactive = Stopwatch.StartNew();
            var consecutiveErrors = 0;

            while (inactive.Elapsed < TenMinutes && consecutiveErrors < TenConsecutiveErrors)
            {
                try
                {
                    if (fileSet.CheckForChangesAndRememberTimestamp())
                    {
                        Console.WriteLine();
                        Console.WriteLine($"{DateTime.Now:T}");

                        inactive.Restart();
                        await ProcessOnce();
                    }

                    consecutiveErrors = 0;
                    await Task.Delay(TwoSeconds).ConfigureAwait(false);
                }
                catch (Exception err)
                {
                    Print.ErrorSummary(err);

                    consecutiveErrors += 1;
                    await Task.Delay(FiveSeconds).ConfigureAwait(false);
                }
            }

            // Warn user that we stopped watching.
            var reason = consecutiveErrors >= TenConsecutiveErrors ? $" There were {TenConsecutiveErrors} consecutive errors." : $" No changes detected for {TenMinutes.TotalMinutes:#,0} minutes.";
            Console.WriteLine($" STOPPING | {reason}");
        }

        static async Task ProcessOnce()
        {
            try
            {
                // WHY?
                // Input and output configurations can change between runs.
                // We create and destroy DI container for each simulation session.
                var simSessionBuilder = Host.CreateEmptyApplicationBuilder(settings: new());

                // Apply default choices and user overrides.
                simSessionBuilder.Configuration
                    .AddSimulationDefaults()
                    .AddReportDefaults()
                    .AddRequiredYamlFile(InputConfigFileName)
                    .AddOptionalYamlFile(OptionalOutputConfigFileName);

                // Assemble simulation and reporting components.
                simSessionBuilder.Services
                    .AddSimulationComponents()
                    .AddReportComponents()
                    ;

                using (var simSession = simSessionBuilder.Build())
                {
                    // Resolve required services
                    ISimulation simulation = simSession.Services.GetRequiredService<ISimulation>();
                    ISimulationReports simReports = simSession.Services.GetRequiredService<ISimulationReports>();

                    // Run simulation
                    var timer = Stopwatch.StartNew();
                    var simResults = simulation.Run();
                    timer.Stop();
                    Print.Milestone("Simulation complete", timer.Elapsed);

                    // Generate reports
                    timer.Restart();
                    await simReports.GenerateAsync(simResults);
                    timer.Stop();
                    Print.Milestone("Reports ready", timer.Elapsed);

                    var sr = simResults.SurvivalRate;
                    var txtSurvivalRate = sr > 0.99 ? $"{sr:P1}" : $"{sr:P0}";
                    Console.WriteLine($" Survival rate: {txtSurvivalRate}");
                }
            }
            catch (Exception warning) when (warning is FatalWarning or ValidationException)
            {
                // WHY: FatalWarning and ValidationException are information, not errors.
                Console.WriteLine(warning.Message);
            }
        }
    }
}

