using Microsoft.Extensions.Configuration;
using NinthBall.Core;
using NinthBall.Reports.PrettyPrint;
using System.Data;
using System.Diagnostics;

namespace UnitTests.WhatIf
{
    public partial class WhatIfSimulations
    {
        [TestMethod]
        public async Task WhatIfDifferentGrowthObjectives()
        {
            const string ReportFileName = "WhatIf-DifferentGrowthObjevtives.md";

            // Base configuration
            var baseConfig = MyBaseConfiguration;

            string[] VaryBy = ["FlatGrowth", "HistoricalGrowth", "RandomHistoricalGrowth", "RandomGrowth"];

            // Simulate in parallel, collect results
            var elapsed = Stopwatch.StartNew();
            var metrics = VaryBy
                .AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .Select(variation => TryOneVariation(baseConfig, variation))
                .ToList();
            elapsed.Stop();

            Console.WriteLine($"{metrics.Count} Simulations | Elapsed: {elapsed.Elapsed.TotalMilliseconds:#,0} mSec");

            using (var writer = File.CreateText(Path.Combine(WhatIfSimulations.ReportsFolder, ReportFileName)))
            {
                writer.PrintMarkdownTitle2($"Different growth objectives");
                PrintMetrics(writer, metrics);
            }

            return;

            static (string GrowthObjective, WhatIfMetrics Metrics) TryOneVariation(IConfiguration baseConfiguration, string growthObjective)
            {
                var overrides = SimInputOverrides
                    .For<SimParams>().Replace(x => x.Objectives, growthObjective, baseConfiguration, s => s.Contains("growth", StringComparison.OrdinalIgnoreCase))
                    .For<BootstrapOptions>().With(x => x.RegimeAwareness, 1.0);

                return (
                    growthObjective, 
                    RunOneSimulation(baseConfiguration, overrides)
                );
            }

            static void PrintMetrics(TextWriter writer, IList<(string GrowthObjective, WhatIfMetrics Metrics)> results)
            {
                var dt = new DataTable()
                    .WithColumn<string>("Growth")
                    .WithColumn<int>("Iterations")
                    .WithColumn<string>("Initial Balance")
                    .WithColumn<string>("Horizon")
                    .WithColumn<string>("Y0-Expense")
                    .WithColumn<double>("SurvivalRate", format: "P0")
                    .WithColumn<double>("Balance(r) 5th", format: "C0")
                    .WithColumn<double>("Balance(r) 10th", format: "C0")
                    .WithColumn<double>("Balance(r) 20th", format: "C0")
                    .WithColumn<double>("Balance(r) 50th", format: "C0");

                foreach (var pair in results)
                {
                    var r = pair.Metrics;

                    dt.AppendRow(
                    [
                        pair.GrowthObjective,
                        r.NumIterations,
                        $"{r.InitialBalance/1000000:C1} M",
                        r.AgeRange,
                        $"{r.Year0Expense/1000:C0} K",
                        r.SurvivalRate,
                        r.RBal05th.RoundToMultiples(1000),
                        r.RBal10th.RoundToMultiples(1000),
                        r.RBal20th.RoundToMultiples(1000),
                        r.RBal50th.RoundToMultiples(1000),
                    ]);
                }

                writer
                    .PrintMarkdownTable(dt)
                    .AppendLine();
            }
        }
    }
}
