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
        public async Task WhatIfInitialBalanceVsFirstYearExpense()
        {
            const string ReportFileName = "WhatIf-InitialBalanceVsFirstYearExpense.md";

            // Base configuration
            var baseConfig = MyBaseConfiguration;

            var options = baseConfig.ReadAndValidateRequiredSection<WhatIfOptions>();

            // Prepare tuples of vary-by inputs
            List<(double InitialBalance, double FirstYearExp)> VaryBy = new();
            for (double ib = options.InitialBalance.Min; ib <= options.InitialBalance.Max; ib += options.InitialBalance.Steps)
                for (double y0Exp = options.FirstYearExpense.Min; y0Exp <= options.FirstYearExpense.Max; y0Exp += options.FirstYearExpense.Steps)
                    VaryBy.Add((ib, y0Exp));

            // Simulate in parallel, collect results
            var elapsed = Stopwatch.StartNew();
            var Results = VaryBy
                .AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .Select(pair => TryOneVariation(baseConfig, pair.InitialBalance, pair.FirstYearExp))
                .OrderBy(x => x.InitialBalance)
                .ThenBy(x => x.Year0Expense)
                .ToList();
            elapsed.Stop();

            Console.WriteLine($"{Results.Count} Simulations | Elapsed: {elapsed.Elapsed.TotalMilliseconds:#,0} mSec");

            using (var writer = File.CreateText(Path.Combine(WhatIfSimulations.ReportsFolder, ReportFileName)))
            {
                var p = baseConfig.ReadAndValidateRequiredSection<SimParams>();
                writer.PrintMarkdownTitle2($"Initial balance vs first year expense | {p.NoOfYears} years | From {p.StartAge} to {p.StartAge + p.NoOfYears}");

                PrintSurvivalMatrix(writer, Results, options.TargetSurvivalRate);
                PrintMetricsForSelectInitialBalance(writer, Results, anchorInitialBalance: options.InitialBalance.Target);
                PrintMetricsForSelectFirstYearExpense(writer, Results, anchorFirstYearExpense: options.FirstYearExpense.Target);
            }

            static WhatIfMetrics TryOneVariation(IConfiguration baseConfiguration, double initialBalance, double firstYearExp)
            {
                var overrides = SimInputOverrides
                    .For<Initial>()
                        .With(x => x.PreTax.Amount, initialBalance / 2)
                        .With(x => x.PostTax.Amount, initialBalance / 2)
                    .For<LivingExpenses>()
                        .With(x => x.FirstYearAmount, firstYearExp);

                return RunOneSimulation(baseConfiguration, overrides);
            }

            static void PrintSurvivalMatrix(TextWriter writer, IList<WhatIfMetrics> results, double targetSurvivalRate)
            {
                // WHY: Do not trust the ordering guarentee from upstream.
                // It is a small list, let us sort it here to avoid pinky-promise.
                results = results.OrderBy(x => x.Year0Expense).ThenBy(x => x.InitialBalance).ToList();

                // FYI: .NET preserves the order when using Distinct()
                // FYI: .NET preserves the order of inner elements when using GroupBy()
                var colLabels = results.Select(x => x.Year0Expense).Distinct().ToList();
                var rowGroups = results
                    .GroupBy(x => x.InitialBalance)
                    .OrderBy(x => x.Key)
                    .ToList();

                var dtMatrix = new DataTable().WithColumn<string>("Initial Balance");
                foreach (var colLabel in colLabels) dtMatrix.WithColumn<string>($"{colLabel / 1000:C1} K");

                foreach (var grp in rowGroups)
                {
                    var rowLabel = grp.Select(x => x.InitialBalance).Distinct().Single();
                    var sRates = grp.Select(x => x.SurvivalRate);

                    var cells = new List<object>();
                    cells.Add($"{rowLabel/1000000:C1} M" );
                    foreach (var r in sRates) cells.Add(r >= targetSurvivalRate ? $"{r:P0}" : string.Empty);
                    dtMatrix.Rows.Add(cells.ToArray());
                }

                writer
                    .PrintMarkdownTitle3("Survival Matrix")
                    .PrintMarkdownTable(dtMatrix)
                    .AppendLine();
            }

            static void PrintMetricsForSelectInitialBalance(TextWriter writer, IList<WhatIfMetrics> results, double anchorInitialBalance)
            {
                var title = $"Initial balance: {anchorInitialBalance:C0} | Different first year expenses:";

                var filteredResults = results
                    .Where(x => x.InitialBalance.AlmostSame(anchorInitialBalance, Precision.Amount))
                    .OrderBy(x => x.Year0Expense)
                    .ToList();

                PrintMetrics(writer, title, filteredResults);
            }

            static void PrintMetricsForSelectFirstYearExpense(TextWriter writer, IList<WhatIfMetrics> results, double anchorFirstYearExpense)
            {
                var title = $"First year expense: {anchorFirstYearExpense:C0} | Different Initial Balances:";

                var filteredResults = results
                    .Where(x => x.Year0Expense.AlmostSame(anchorFirstYearExpense, Precision.Amount))
                    .OrderBy(x => x.InitialBalance)
                    .ToList();

                PrintMetrics(writer, title, filteredResults);
            }

            static void PrintMetrics(TextWriter writer, string title, IList<WhatIfMetrics> filteredResults)
            {
                var dt = new DataTable()
                    .WithColumn<string>("Initial Balance")
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
                        $"{r.InitialBalance/1000000:C1} M",
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
