
using Microsoft.Extensions.Configuration;
using NinthBall.Core;
using NinthBall.Templates;
using System.Diagnostics;


namespace NinthBall
{
    internal sealed class App(IConfiguration config, SimRunner simRunner)
    {
        static readonly TimeSpan TwoSeconds  = TimeSpan.FromSeconds(2);
        static readonly TimeSpan FiveSeconds = TimeSpan.FromSeconds(5);
        static readonly TimeSpan TenMinutes  = TimeSpan.FromMinutes(10);

        string InputFileName  => config.GetValue<string>("In") ?? throw new Exception("Input file name not specified.");
        string FallbackOutputFileName => config.GetValue<string>("Output") ?? "./SimReport.html";
        bool WatchMode => CmdLine.Switch("watch");

        public async Task RunAsync()
        {
            if (WatchMode) await ProcessForever(InputFileName); else await ProcessOnce(InputFileName);
        }

        async Task ProcessForever(string inputFileName)
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
                        await ProcessOnce(inputFileName);
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

        async Task ProcessOnce(string inputFileName)
        {
            Console.WriteLine();

            string outputFileName = FallbackOutputFileName;

            try
            {
                // Load Config
                var simConfig = SimInputReader.FromYamlFile(inputFileName);
                outputFileName = simConfig.Output ?? FallbackOutputFileName;

                // Ensure output directory.
                var outputDir = Path.GetDirectoryName(outputFileName) ?? "./";
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

                // Run simulation
                var timer = Stopwatch.StartNew();
                var simResult = simRunner.Run(simConfig);
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

        //private SimInput LoadConfig(string path)
        //{
        //    const string MyPathTag = "$(MyPath)";

        //    var yamlText = File.ReadAllText(path);

        //    // Replace $(MyPath)
        //    if (yamlText.Contains(MyPathTag, StringComparison.OrdinalIgnoreCase))
        //    {
        //        var myPath = Path.GetFullPath(Path.GetDirectoryName(path) ?? "./")
        //            .Replace('\\', '/')
        //            .TrimEnd('/');

        //        yamlText = yamlText.Replace(MyPathTag, myPath, StringComparison.OrdinalIgnoreCase);
        //    }

        //    var jsonText = MyHost.YamlTextToJsonText(yamlText);
            
        //    var options = new System.Text.Json.JsonSerializerOptions
        //    {
        //        PropertyNameCaseInsensitive = true,
        //        ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,
        //        AllowTrailingCommas = true,
        //        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
        //    };

        //    options.Converters.Add(new PercentageToDoubleConverter());
        //    options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

        //    return System.Text.Json.JsonSerializer.Deserialize<SimInput>(jsonText, options) 
        //        ?? throw new Exception("Failed to deserialize SimInput.");
        //}
    }
}
