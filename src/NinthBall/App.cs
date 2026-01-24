
using NinthBall.Core;
using NinthBall.Outputs;
using NinthBall.Outputs.Html;
using NinthBall.Outputs.Excel;
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
        bool PrintHelp => CmdLine.Switch("help");
        bool SampleYamls => CmdLine.Switch("sampleyamls");

        public async Task RunAsync()
        {
            if (PrintHelp)
            {
                Print.Help();
            }
            else if (SampleYamls)
            {
                ExportSampleYamlConfigFiles();
            }
            else
            {
                // Ensure output directory.
                var outputDir = Path.GetDirectoryName(OutputFileName) ?? "./";
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

                // Process
                if (WatchMode) await ProcessForever(); else await ProcessOnce();
            }
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
                var inputConfig  = SimInputReader.ReadFromYamlFile(InputFileName);
                var outputConfig = SimOutputReader.ReadFromYamlFile(LocateOutputConfigFile(InputFileName)).ToSimOutput();

                // Run simulation
                var timer = Stopwatch.StartNew();
                var simResult = SimEngine.Run(inputConfig);
                timer.Stop();

                var htmlFileName = OutputFileName;
                await HtmlOutput.GenerateAsync(Services, simResult, InputFileName, htmlFileName, outputConfig);
                Console.WriteLine($" Html report  | See {htmlFileName}");

                try
                {
                    var excelFileName = Path.ChangeExtension(htmlFileName, ".xlsx");
                    await ExcelOutput.Generate(simResult, excelFileName, outputConfig);
                    Console.WriteLine($" Excel report | See {excelFileName}");
                }
                catch(System.IO.IOException ioErr)
                {
                    // Excel file is probably currently open.
                    Console.WriteLine(" WARNING: Excel report not generated, if present, may not agree with html report.");
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
        
            static string LocateOutputConfigFile(string inputFileName)
            {
                ArgumentNullException.ThrowIfNull(inputFileName);

                // Use the input file name directory as reasonable starting point
                var dir = Path.GetDirectoryName(inputFileName) ?? "./";

                // Convention based output configuration file names.
                string[] possibleSimOutputYamlFileNames =
                [
                    Path.Combine(dir, Path.GetFileNameWithoutExtension(inputFileName) + ".output.yaml"),
                    Path.Combine(dir,  "SimOutput.yaml"),
                    Path.Combine("./", "SimOutput.yaml"),
                ];

                foreach (var candidateFileName in possibleSimOutputYamlFileNames)
                {
                    if (File.Exists(candidateFileName))
                    {
                        Console.WriteLine($"Using {Path.GetFullPath(candidateFileName)}");
                        return candidateFileName;
                    }
                }

                throw new FatalWarning($"Can't locate SimOutput.yaml");
            }
        
        }
    
        void ExportSampleYamlConfigFiles()
        {
            ExportSampleYaml("SampleInput.yaml", "./SampleInput.yaml");
            ExportSampleYaml("SimOutput.yaml",   "./SimOutput.yaml");

            static void ExportSampleYaml(string resNameEndsWith, string outputFileName)
            {
                var resourceName = typeof(App).Assembly.GetManifestResourceNames().Where(x => x.EndsWith(resNameEndsWith, StringComparison.OrdinalIgnoreCase)).Single();
                using var resStream = typeof(App).Assembly.GetManifestResourceStream(resourceName) ?? throw new Exception("Unexpected | Resource stream was null.");
                using var reader = new StreamReader(resStream);
                
                var sampleYaml = reader.ReadToEnd();
                File.WriteAllText(outputFileName, sampleYaml);

                Console.WriteLine($" See: {Path.GetFullPath(outputFileName)}");
            }
        }
    }
}
 