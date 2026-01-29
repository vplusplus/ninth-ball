
using NinthBall.Core;
using NinthBall.OutputsV2;
using NinthBall.Utils;
using System.Diagnostics;

namespace NinthBall
{
    internal sealed class App
    {
        static readonly TimeSpan TwoSeconds  = TimeSpan.FromSeconds(2);
        static readonly TimeSpan FiveSeconds = TimeSpan.FromSeconds(5);
        static readonly TimeSpan TenMinutes  = TimeSpan.FromMinutes(10);

        string InputConfigFileName  => Path.GetFullPath(CmdLine.Required("In"));
        string OutputConfigFileName => Path.GetFullPath(CmdLine.Required("out"));

        bool WatchMode => CmdLine.Switch("watch");
        bool PrintHelp => CmdLine.Switch("help");

        public async Task RunAsync()
        {
            if (PrintHelp)
            {
                Print.Help();
            }
            else
            {
                if (WatchMode) await ProcessForever(); else await ProcessOnce();
            }
        }

        async Task ProcessForever()
        {
            var fileSet = new WatchFileSet(InputConfigFileName, OutputConfigFileName);

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
                var timer = Stopwatch.StartNew();
                var simResult = SimEngine.Run(InputConfigFileName);
                timer.Stop();

                SimOutputEngine.Generate(simResult, OutputConfigFileName);


                // Inform                
                Print.Done(simResult, timer.Elapsed);
            }
            catch (Exception err)
            {
                // Print essentials to console
                Print.ErrorSummary(err);
            }
        
        
        }

    }
}

