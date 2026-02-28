using Microsoft.Extensions.Configuration;
using NinthBall.Core;
using NinthBall.Reports.PrettyPrint;
using System.Data;

namespace UnitTests.WhatIf
{
    public partial class MultipleSimulations
    {
        [TestMethod]
        public async Task FindMinBalanceAndMaxExpense()
        {
            const string ReportFileName = "MinBalanceAndMaxExpense.md";

            // Given initial balance, find first year expense
            const double InitBalance    = 3_500_000;
            const double MinExpense     =   100_000;
            const double MaxExpense     =   200_000;
            const double ExpSteps       =    10_000;

            // Given first year expense, find initial balance.
            const double FirstYearExp   =   130_000;
            const double MinBalance     = 2_000_000;
            const double MaxBalance     = 4_000_000;
            const double BalanceSteps   =   250_000;

            const double MinSurvivalRate = 0.9;

            // Base configuration
            var baseConfig = MyBaseConfiguration;

            using (var writer = File.CreateText(Path.Combine(MultipleSimulations.ReportsFolder, ReportFileName)))
            {
                var p = baseConfig.ReadAndValidateRequiredSestion<SimParams>();
                writer.PrintMarkdownTitle3($"From {p.StartAge} to {p.StartAge + p.NoOfYears} | {p.NoOfYears} years");

                using var w111 = new StringWriter();
                using var w222 = new StringWriter();
                using var w333 = new StringWriter();

                var t1 = TrySurvivalMatrix(w111, baseConfig);
                var t2 = TryDifferentFirstYearExpense(w222, baseConfig);
                var t3 = TryDifferentInitialBalance(w333, baseConfig);

                await Task.WhenAll(t1, t2, t3);

                writer.Write(w111.ToString());
                writer.Write(w222.ToString());
                writer.Write(w333.ToString());
            }

            return;

            static async Task TrySurvivalMatrix(TextWriter writer, IConfiguration baseConfiguration)
            {
                await Task.Yield();

                Console.WriteLine($"TrySurvivalMatrix - Begin @ {DateTime.UtcNow:hh:mm:ss}");

                double[] FirstYearExpenses = [120000, 130000, 140000, 150000, 160000];
                double[] InitBalances = [ 2.0, 2.5, 3.0, 3.5, 4.0, 4.5, 5.0 ];

                var dtMatrix = new DataTable();
                dtMatrix.WithColumn<string>("First year Exp");
                foreach (var ib in InitBalances) dtMatrix.WithColumn<double>($"{ib:C1} M", format: "P0");

                for (int fe = 0; fe < FirstYearExpenses.Length; fe++)
                {
                    var firstYearExp = FirstYearExpenses[fe];

                    var cells = new List<object>();
                    cells.Add($"{firstYearExp:C0}");

                    for (int ib = 0; ib < InitBalances.Length; ib++)
                    {
                        var initBalance  = InitBalances[ib] * 1000000;

                        var overrides = SimInputOverrides
                            .For<Initial>()
                                .With(x => x.PreTax.Amount, initBalance / 2)
                                .With(x => x.PostTax.Amount, initBalance / 2)
                            .For<LivingExpenses>()
                                .With(x => x.FirstYearAmount, firstYearExp);

                        var survivalRate = RunSimulation(baseConfiguration, overrides).SurvivalRate;
                        cells.Add(survivalRate);
                    }

                    dtMatrix.Rows.Add(cells.ToArray());
                }

                writer
                    .PrintMarkdownTitle2("Survival matrix - Init balance vs First year exp")
                    .PrintMarkdownTable(dtMatrix)
                    .AppendLine();

                Console.WriteLine($"TrySurvivalMatrix - Done @ {DateTime.UtcNow:hh:mm:ss}");
            }

            static async Task TryDifferentFirstYearExpense(TextWriter writer, IConfiguration baseConfiguration)
            {
                await Task.Yield();

                Console.WriteLine($"TryDifferentFirstYearExpense - Begin @ {DateTime.UtcNow:hh:mm:ss}");

                // Prepare output table
                var dt = new DataTable()
                    .WithColumn<double>("Y0-Expense", format: "C0")
                    .WithColumn<double>("SurvivalRate", format: "P0")
                    .WithColumn<double>("Balance(r) 5th", format: "C0")
                    .WithColumn<double>("Balance(r) 10th", format: "C0")
                    .WithColumn<double>("Balance(r) 20th", format: "C0")
                    ;

                // Try different first-year-expense
                for (double expense = MinExpense; expense <= MaxExpense; expense += ExpSteps)
                {
                    var overrides = SimInputOverrides
                        .For<Initial>()
                            .With(x => x.PreTax.Amount, InitBalance / 2)
                            .With(x => x.PostTax.Amount, InitBalance / 2)
                        .For<LivingExpenses>()
                            .With(x => x.FirstYearAmount, expense);

                    var simResult = RunSimulation(baseConfiguration, overrides);

                    dt.AppendRow(
                    [
                        expense,
                        simResult.SurvivalRate,
                        simResult.IterationAtPercentile(0.05).EndingBalanceReal,
                        simResult.IterationAtPercentile(0.10).EndingBalanceReal,
                        simResult.IterationAtPercentile(0.20).EndingBalanceReal
                    ]);
                }

                var init = baseConfiguration.ReadAndValidateRequiredSestion<Initial>();
                writer
                    .PrintMarkdownTitle2("Frst year expense vs survival rate")
                    .PrintMarkdownTitle3($"Initial balance: {init.PreTax.Amount + init.PostTax.Amount:C0}")
                    .PrintMarkdownTitle3("Results:")
                    .PrintMarkdownTable(dt)
                    .AppendLine();

                Console.WriteLine($"TryDifferentFirstYearExpense - Done @ {DateTime.UtcNow:hh:mm:ss}");
            }

            static async Task TryDifferentInitialBalance(TextWriter writer, IConfiguration baseConfiguration)
            {
                await Task.Yield();

                Console.WriteLine($"TryDifferentInitialBalance - Begin @ {DateTime.UtcNow:hh:mm:ss}");

                // Prepare output table
                var dt = new DataTable()
                    .WithColumn<double>("InitialBalance", format: "C0")
                    .WithColumn<double>("SurvivalRate", format: "P0")
                    .WithColumn<double>("Balance(r) 5th", format: "C0")
                    .WithColumn<double>("Balance(r) 10th", format: "C0")
                    .WithColumn<double>("Balance(r) 20th", format: "C0")
                    ;

                // Sweep range
                for (double initialBalance = MinBalance; initialBalance <= MaxBalance; initialBalance += BalanceSteps)
                {
                    var overrides = SimInputOverrides
                        .For<Initial>()
                            .With(x => x.PreTax.Amount, initialBalance / 2)
                            .With(x => x.PostTax.Amount, initialBalance / 2)
                        .For<LivingExpenses>()
                            .With(x => x.FirstYearAmount, FirstYearExp);

                    var simResult = MultipleSimulations.RunSimulation(baseConfiguration, overrides);

                    if (simResult.SurvivalRate < MinSurvivalRate) continue;

                    dt.AppendRow(
                    [
                        initialBalance,
                        simResult.SurvivalRate,
                        simResult.IterationAtPercentile(0.05).EndingBalanceReal,
                        simResult.IterationAtPercentile(0.10).EndingBalanceReal,
                        simResult.IterationAtPercentile(0.20).EndingBalanceReal
                    ]);
                }

                var exp = baseConfiguration.ReadAndValidateRequiredSestion<LivingExpenses>();
                writer
                    .PrintMarkdownTitle2("Initial Balance vs Survival Rate")
                    .PrintMarkdownTitle3($"First year expense: {exp.FirstYearAmount:C0}")

                    .PrintMarkdownTitle3("Results")
                    .PrintMarkdownTable(dt)
                    .AppendLine();

                Console.WriteLine($"TryDifferentInitialBalance - Done @ {DateTime.UtcNow:hh:mm:ss}");
            }

        }
    }
}
