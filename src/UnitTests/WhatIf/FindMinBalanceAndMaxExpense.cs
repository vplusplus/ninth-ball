using Microsoft.Extensions.Configuration;
using NinthBall.Core;
using NinthBall.Reports.PrettyPrint;
using System.Data;
using System.Diagnostics;

namespace UnitTests.WhatIf
{
    using MultiSimResult = (double InitBal, double FYExp, double SurvivalRate, double RBal5th, double RBal10th, double RBal20th);

    public partial class MultipleSimulations
    {
        [TestMethod]
        public async Task FindMinBalanceAndMaxExpense()
        {
            const string ReportFileName = "MinBalanceAndMaxExpense.md";

            // Given initial balance, find first year expense
            const double MinExpense     =   120_000;
            const double MaxExpense     =   180_000;
            const double ExpSteps       =    10_000;

            // Given first year expense, find initial balance.
            const double MinBalance     = 2_000_000;
            const double MaxBalance     = 5_000_000;
            const double BalanceSteps   =   500_000;

            // Prepare tuples of vary-by inputs
            List<(double InitBalance, double FirstYearExp)> VaryBy = new();
            for (double ib = MinBalance; ib <= MaxBalance; ib += BalanceSteps)
                for (double fye = MinExpense; fye <= MaxExpense; fye += ExpSteps)
                    VaryBy.Add((ib, fye));

            // Base configuration
            var baseConfig = MyBaseConfiguration;

            // Simulate in parallel, collect results
            var elapsed = Stopwatch.StartNew();
            var Results = VaryBy
                .AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .Select(pair => TryOneVariation(baseConfig, pair.InitBalance, pair.FirstYearExp))
                .OrderBy(x => x.InitBal)
                .ThenBy(x => x.FYExp)
                .ToList();
            elapsed.Stop();

            Console.WriteLine($"{Results.Count} Simulations | Elapsed: {elapsed.Elapsed.TotalMilliseconds:#,0} mSec");

            using (var writer = File.CreateText(Path.Combine(MultipleSimulations.ReportsFolder, ReportFileName)))
            {
                var p = baseConfig.ReadAndValidateRequiredSestion<SimParams>();
                writer.PrintMarkdownTitle3($"From {p.StartAge} to {p.StartAge + p.NoOfYears} | {p.NoOfYears} years");

                PrintSurvivalMatrix(writer, Results);
                PrintMetricsForSelectInitialBalance(writer, Results, anchorInitialBalance: 3_000_000);
                PrintMetricsForSelectFirstYearExpense(writer, Results, anchorFirstYearExpense: 130_000);
            }

            // Test one input combination, collect key metrics from the Simulation result
            static MultiSimResult TryOneVariation(IConfiguration baseConfiguration, double initialBalance, double firstYearExp)
            {
                var overrides = SimInputOverrides
                    .For<Initial>()
                        .With(x => x.PreTax.Amount, initialBalance / 2)
                        .With(x => x.PostTax.Amount, initialBalance / 2)
                    .For<LivingExpenses>()
                        .With(x => x.FirstYearAmount, firstYearExp);

                var simResult = RunSimulation(baseConfiguration, overrides);

                return new MultiSimResult
                {
                    InitBal = initialBalance,
                    FYExp = firstYearExp,
                    SurvivalRate = simResult.SurvivalRate,
                    RBal5th = simResult.IterationAtPercentile(0.05).EndingBalanceReal,
                    RBal10th = simResult.IterationAtPercentile(0.10).EndingBalanceReal,
                    RBal20th = simResult.IterationAtPercentile(0.20).EndingBalanceReal
                };
            }

            static void PrintSurvivalMatrix(TextWriter writer, IList<MultiSimResult> results)
            {
                // WHY: Do not trust the ordering guarentee from upstream.
                // It is a small list, let us sort it here to avoid pinky-promise.
                results = results.OrderBy(x => x.FYExp).ThenBy(x => x.InitBal).ToList();

                // FYI: .NET preserves the order when using Distinct()
                // FYI: .NET preserves the order of inner elements when using GroupBy()
                var colLabels = results.Select(x => x.InitBal).Distinct().ToList();
                var rowGroups = results
                    .GroupBy(x => x.FYExp)
                    .OrderBy(x => x.Key)
                    .ToList();

                var dtMatrix = new DataTable().WithColumn<string>("First Year Exp");
                foreach (var colLabel in colLabels) dtMatrix.WithColumn<double>( $"{colLabel/1000000:C1} M", format: "P0");

                foreach(var grp in rowGroups)
                {
                    var fyExp = grp.Key;
                    var sRates = grp.Select(x => x.SurvivalRate);

                    var cells = new List<object>();
                    cells.Add($"{fyExp/1000:C0} K");
                    foreach (var r in sRates) cells.Add(r);

                    dtMatrix.Rows.Add(cells.ToArray());
                }

                writer
                    .PrintMarkdownTitle2("Survival matrix - Init balance vs First year exp")
                    .PrintMarkdownTable(dtMatrix)
                    .AppendLine();

            }

            static void PrintMetricsForSelectInitialBalance(TextWriter writer, IList<MultiSimResult> results, double anchorInitialBalance)
            {
                var filteredResults = results
                    .Where(x => x.InitBal == anchorInitialBalance)
                    .OrderBy(x => x.FYExp)
                    .ToList();

                var dt = new DataTable()
                    .WithColumn<double>("Y0-Expense", format: "C0")
                    .WithColumn<double>("SurvivalRate", format: "P0")
                    .WithColumn<double>("Balance(r) 5th", format: "C0")
                    .WithColumn<double>("Balance(r) 10th", format: "C0")
                    .WithColumn<double>("Balance(r) 20th", format: "C0");

                foreach (var r in filteredResults)
                {
                    dt.AppendRow(
                    [
                        r.FYExp,
                        r.SurvivalRate,
                        r.RBal5th,
                        r.RBal10th,
                        r.RBal20th
                    ]);
                }

                writer
                    .PrintMarkdownTitle2("First year expense vs survival rate")
                    .PrintMarkdownTitle3($"Initial balance: {anchorInitialBalance:C0}")
                    .PrintMarkdownTitle3("Results:")
                    .PrintMarkdownTable(dt)
                    .AppendLine();
            }

            static void PrintMetricsForSelectFirstYearExpense(TextWriter writer, IList<MultiSimResult> results, double anchorFirstYearExpense)
            {
                var filteredResults = results
                    .Where(x => x.FYExp == anchorFirstYearExpense)
                    .OrderBy(x => x.InitBal)
                    .ToList();

                var dt = new DataTable()
                    .WithColumn<string>("InitialBalance")
                    .WithColumn<double>("SurvivalRate", format: "P0")
                    .WithColumn<double>("Balance(r) 5th", format: "C0")
                    .WithColumn<double>("Balance(r) 10th", format: "C0")
                    .WithColumn<double>("Balance(r) 20th", format: "C0");

                foreach (var r in filteredResults)
                {
                    dt.AppendRow(
                    [
                        $"{r.InitBal / 1000000:C1} M",
                        r.SurvivalRate,
                        r.RBal5th,
                        r.RBal10th,
                        r.RBal20th
                    ]);
                }

                writer
                    .PrintMarkdownTitle2("Initial Balance vs Survival Rate")
                    .PrintMarkdownTitle3($"First year expense: {anchorFirstYearExpense:C0}")
                    .PrintMarkdownTitle3("Results")
                    .PrintMarkdownTable(dt)
                    .AppendLine();
            }
        }
    }
}


