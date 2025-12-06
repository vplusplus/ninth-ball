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
            var fileSet = new WatchFileSet(inputFileName);

            if (CmdLine.Switch("whatif"))
            {
                var dir = Path.GetDirectoryName(inputFileName) ?? "./";
                fileSet.AlsoWatch(
                    Path.Combine(dir, "Ratings.yaml")
                    // Path.Combine(dir, "WhatIf.yaml")
                );
            }

            Console.WriteLine($" WATCHING:");
            foreach (var file in fileSet.Watching) Console.WriteLine($"   {Path.GetFullPath(file)}");
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

                    // Check if any file changed
                    bool somethingChanged = fileSet.CheckForChangesAndRememberTimestamp();
                    if (somethingChanged)
                    {
                        await ProcessOnce();
                        errorCount = 0;
                    }

                    // Wait
                    await Task.Delay(TwoSeconds).ConfigureAwait(false);
                }
                catch(FatalWarning)
                {
                    throw;
                }
                catch(Exception err)
                {
                    if (++errorCount > 2) throw new Exception("Too many errors. Stopped watching.", err);
                    await Task.Delay(FiveSeconds).ConfigureAwait(false);
                }
            }
        }

        static async Task ProcessOnce()
        {
            var inputFileName = CmdLine.Required("in");
            var isWhatIf = CmdLine.Switch("whatif");

            if (isWhatIf)
            {
                Console.WriteLine("WhatIf is currently stubbed...");
                //await ProcessWhatIf(inputFileName);
            }
            else
            {
                await ProcessSimulation(inputFileName);
            }
        }

        static async Task ProcessSimulation(string inputFileName)
        {
            var outputFileName = "./Output.html";

            try
            {
                var dir = Path.GetDirectoryName(inputFileName) ?? "./";
                var ratingsFile = Path.Combine(dir, "Ratings.yaml");

                // FromYamlFile Simulation configurations
                var simConfig = SimConfigReader.Read(inputFileName);
                
                // Capture output file name here before proceeding
                outputFileName = simConfig.Output;
                var outputDir = Path.GetDirectoryName(outputFileName) ?? "./";
                if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

                // Validate and print the params 
                simConfig.ThrowIfInvalid().PrintParams();

                // Run simulation
                var timer = Stopwatch.StartNew();
                var simResult = Simulation.RunSimulation(simConfig); //.WithScores(simRatings.AvailableRatings());
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

        //static async Task ProcessWhatIf(string inputFileName)
        //{
        //    var outputFileName = "./Output.html";

        //    try
        //    {
        //        var dir = Path.GetDirectoryName(inputFileName) ?? "./";
        //        var ratingsFile = Path.Combine(dir, "Ratings.yaml");
        //        var whatIfFile = Path.Combine(dir, "WhatIf.yaml");

        //        // FromYamlFile all configurations
        //        var simConfig = SimConfigReader.Read(inputFileName);
        //        var ratingsConfig = SimRatingsReader.Read(ratingsFile);
        //        var whatIfConfig = WhatIfConfigReader.Read(whatIfFile);

        //        // Capture output file name
        //        outputFileName = simConfig.Output;
        //        var outputDir = Path.GetDirectoryName(outputFileName) ?? "./";
        //        if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

        //        // Build optimization problem
        //        var builder = new SimConfigBuilder(inputFileName);
        //        var ratings = RatingsBuilder.CreateRatings(ratingsConfig);
        //        var problem = WhatIfBuilder.CreateProblem(whatIfConfig, builder, ratings);

        //        // Print configuration
        //        Console.WriteLine($" OPTIMIZATION MODE");
        //        Console.WriteLine($" Variables: {string.Join(", ", problem.SearchVariables)}");
        //        Console.WriteLine($" Ratings: {string.Join(", ", problem.Ratings.Select(r => r.Name))}");
        //        Console.WriteLine();

        //        // Run optimization with progress
        //        var timer = Stopwatch.StartNew();
        //        var progress = new Progress<(int current, int total)>(p =>
        //            Console.Write($"\r Optimizing... {p.current}/{p.total} ({100.0 * p.current / p.total:F1}%)"));

        //        var result = UnifiedSolver.Solve(problem, progress);
        //        timer.Stop();
        //        Console.WriteLine();  // New line after progress

        //        // Generate optimization report
        //        var html = await MyTemplates.GenerateOptimizationReportAsync(result).ConfigureAwait(false);
        //        File.WriteAllText(outputFileName, html);

        //        // Print results
        //        Console.WriteLine($" Found {result.ParetoFront.Count} Pareto-optimal solutions");
        //        Console.WriteLine($" Total evaluations: {result.TotalEvaluations}");
        //        Console.WriteLine($" Time elapsed: {timer.Elapsed.TotalSeconds:F1}s");
        //        Console.WriteLine($" Output: {outputFileName}");
        //        Console.WriteLine();
        //    }
        //    catch (FatalWarning warn)
        //    {
        //        SaveErrorHtml(warn, outputFileName);
        //        Console.WriteLine($" FATAL WARNING: {warn.Message}");
        //        Console.WriteLine();
        //    }
        //    catch (Exception err)
        //    {
        //        SaveErrorHtml(err, outputFileName);
        //        Print.Error(err);
        //        throw;
        //    }
        //}

        static void SaveErrorHtml(Exception err, string outputFileName)
        {
            var html = MyTemplates.GenerateErrorHtmlAsync(err).ConfigureAwait(false).GetAwaiter().GetResult();
            File.WriteAllText(outputFileName, html);
        }
    }


}
