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
        public async Task WhatIfStartAgeVsFirstYearExpense()
        {
            const string ReportFileName = "WhatIf-StartAgeVsFirstYearExpense.md";

            // Base configuration
            var baseConfig = MyBaseConfiguration;
            var baseParams = baseConfig.ReadAndValidateRequiredSection<SimParams>();
            var options = baseConfig.ReadAndValidateRequiredSection<WhatIfOptions>();

            // Prepare tuples of vary-by inputs
            List<(int StartAge, int NumYears, double FirstYearExp)> VaryBy = new();

            for (int startAge = (int)options.StartAge.Min; startAge <= options.StartAge.Max; startAge += (int)options.StartAge.Steps)
            {
                for (double y0Exp = options.FirstYearExpense.Min; y0Exp <= options.FirstYearExpense.Max; y0Exp += options.FirstYearExpense.Steps)
                {
                    var adjustedNumYears = baseParams.NoOfYears - (startAge - baseParams.StartAge);
                    VaryBy.Add((startAge, adjustedNumYears, y0Exp));
                }
            }

            // Simulate in parallel, collect results
            var elapsed = Stopwatch.StartNew();
            var metrics = VaryBy
                .AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .Select(variation => TryOneVariation(baseConfig, variation.StartAge, variation.NumYears, variation.FirstYearExp))
                .OrderBy(x => x.StartAge)
                .ThenBy(x => x.Year0Expense)
                .ToList();
            elapsed.Stop();

            Console.WriteLine($"{metrics.Count} Simulations | Elapsed: {elapsed.Elapsed.TotalMilliseconds:#,0} mSec");

            using (var writer = File.CreateText(Path.Combine(WhatIfSimulations.ReportsFolder, ReportFileName)))
            {
                var ini = baseConfig.ReadAndValidateRequiredSection<Initial>();
                var initialBalance = ini.PreTax.Amount + ini.PostTax.Amount;
                writer.PrintMarkdownTitle2($"Delayed start vs first year expense | Initial: {initialBalance / 1000000:C1} M");

                PrintSurvivalMatrix(writer, metrics, options.TargetSurvivalRate);
                PrintMetricsForSelectStartAge(writer, metrics, targetStartAge: (int)options.StartAge.Target);
                PrintMetricsForSelectFirstYearExpense(writer, metrics, targetFirstYearExp: options.FirstYearExpense.Target);
            }

            return;

            static WhatIfMetrics TryOneVariation(IConfiguration baseConfiguration, int startAge, int numYears, double firstYearExp)
            {
                var overrides = SimInputOverrides
                    .For<SimParams>()
                        .With(x => x.StartAge, startAge)
                        .With(x => x.NoOfYears, numYears)
                    .For<LivingExpenses>()
                        .With(x => x.FirstYearAmount, firstYearExp);

                return RunOneSimulation(baseConfiguration, overrides);
            }

            static void PrintSurvivalMatrix(TextWriter writer, IList<WhatIfMetrics> results, double targetSurvivalRate)
            {
                // WHY: Do not trust the ordering guarentee from upstream.
                // It is a small list, let us sort it here to avoid pinky-promise.
                results = results.OrderBy(x => x.StartAge).ThenBy(x => x.Year0Expense).ToList();

                // FYI: .NET preserves the order when using Distinct()
                // FYI: .NET preserves the order of inner elements when using GroupBy()
                var colLabels = results.Select(x => x.Year0Expense).Distinct().ToList();
                var rowGroups = results
                    .GroupBy(x => (x.StartAge, x.NumYears))
                    .OrderBy(x => x.Key)
                    .ToList();

                var dtMatrix = new DataTable().WithColumn<string>("Start Age");
                foreach (var colLabel in colLabels) dtMatrix.WithColumn<string>( $"{colLabel/1000:C0} K");

                foreach(var grp in rowGroups)
                {
                    var rowLabel = grp.Select(x => x.AgeRange).Distinct().Single();
                    var sRates   = grp.Select(x => x.SurvivalRate);

                    var cells = new List<object>();
                    cells.Add(rowLabel);
                    foreach (var r in sRates) cells.Add(r >= targetSurvivalRate ? $"{r:P0}" : string.Empty);
                    dtMatrix.Rows.Add(cells.ToArray());
                }

                writer
                    .PrintMarkdownTitle3("Survival Matrix")
                    .PrintMarkdownTable(dtMatrix)
                    .AppendLine();

            }

            static void PrintMetricsForSelectStartAge(TextWriter writer, IList<WhatIfMetrics> results, int targetStartAge)
            {
                var title = $"Start at {targetStartAge} | Different first year expenses:";

                var filteredResults = results
                    .Where(x => x.StartAge == targetStartAge)
                    .OrderBy(x => x.Year0Expense)
                    .ToList();

                PrintMetrics(writer, title, filteredResults);
            }

            static void PrintMetricsForSelectFirstYearExpense(TextWriter writer, IList<WhatIfMetrics> results, double targetFirstYearExp)
            {
                var title = $"First year expense: {targetFirstYearExp:C0} | Different start ages:";

                var filteredResults = results
                    .Where(x => x.Year0Expense.AlmostSame(targetFirstYearExp, Precision.Amount))
                    .OrderBy(x => x.StartAge)
                    .ToList();

                PrintMetrics(writer, title, filteredResults);
            }

            static void PrintMetrics(TextWriter writer, string title, IList<WhatIfMetrics> filteredResults)
            {
                var dt = new DataTable()
                    .WithColumn<string>("Start Age")
                    .WithColumn<string>("Y0-Expense")
                    .WithColumn<double>("SurvivalRate", format: "P0")
                    .WithColumn<double>("Balance(r) 5th", format: "C0")
                    .WithColumn<double>("Balance(r) 10th", format: "C0")
                    .WithColumn<double>("Balance(r) 20th", format: "C0")
                    .WithColumn<double>("Balance(r) 50th", format: "C0");

                foreach (var r in filteredResults)
                {
                    dt.AppendRow(
                    [
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
                    .PrintMarkdownTitle3(title)
                    .PrintMarkdownTable(dt)
                    .AppendLine();
            }

        }
    }
}
