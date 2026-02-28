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
        public async Task FindMinBalanceAndMaxExpenseV2()
        {
            const string ReportFileName = "MinBalanceAndMaxExpenseV2.md";

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
                var colLabels = results.Select(x => x.InitBal).Distinct().OrderBy(x => x).ToList();
                var rowLabels = results.Select(x => x.FYExp).Distinct().OrderBy(x => x).ToList();
                var rowGroups = results
                    .GroupBy(x => x.FYExp)
                    .OrderBy(x => x.Key)
                    .ToList();

                var dtMatrix = new DataTable().WithColumn<string>("First Year Exp");
                foreach (var colLabel in colLabels) dtMatrix.WithColumn<double>( $"{colLabel/1000000:C1} M", format: "P0");

                foreach(var grp in rowGroups)
                {
                    var fyExp = grp.Key;
                    var sRates = grp.Select(x => x.SurvivalRate).OrderBy(x => x).ToArray();

                    var cells = new List<object>();
                    
                    cells.Add($"{fyExp:C0}");
                    foreach (var r in sRates) cells.Add(r);
                    //cells.AddRange(sRates);

                    //cells.Add(grp.Key);
                    //cells.AddRange(grp.Select(x => x.SurvivalRate).OrderBy(x => x));
                    dtMatrix.Rows.Add(cells.ToArray());
                }

                writer
                    .PrintMarkdownTitle2("Survival matrix - Init balance vs First year exp")
                    .PrintMarkdownTable(dtMatrix)
                    .AppendLine();

            }

        }
    }
}


