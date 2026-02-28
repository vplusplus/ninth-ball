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

                TryDifferentFirstYearExpense(writer, baseConfig);
                TryDifferentInitialBalance(writer, baseConfig);
            }

            return;
        
            static void TryDifferentFirstYearExpense(TextWriter writer, IConfiguration baseConfiguration)
            {
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
            }


            static void TryDifferentInitialBalance(TextWriter writer, IConfiguration baseConfiguration)
            {
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

            }

        }
    }
}
