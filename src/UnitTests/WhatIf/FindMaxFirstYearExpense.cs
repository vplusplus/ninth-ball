using Microsoft.Extensions.Configuration;
using NinthBall.Core;
using NinthBall.Reports.PrettyPrint;
using System.Data;
using System.Diagnostics;

namespace UnitTests.WhatIf
{
    [TestClass]
    public class FindMaxFirstYearExpense
    {
        [TestMethod]
        public async Task SweepFirstYearExpense()
        {
            const string ReportFileName = "MaxFirstYearExpense.md";

            // Base configuration (same as MultipleSimulations)
            var baseConfig = new ConfigurationBuilder()
                .AddSimulationDefaults()
                .AddYamlResources(typeof(FindMaxFirstYearExpense).Assembly, ".TestInputs.")
                .Build();

            // Read required sections (only for reporting)
            var p = baseConfig.ReadAndValidateRequiredSestion<SimParams>();
            var init = baseConfig.ReadAndValidateRequiredSestion<Initial>();
            var exp = baseConfig.ReadAndValidateRequiredSestion<LivingExpenses>();

            // Prepare output table
            var dt = new DataTable()
                .WithColumn<string>("Expense")
                .WithColumn<double>("SurvivalRate", format: "P0")
                .WithColumn<double>("10th PCTL", format: "C0")
                .WithColumn<double>("20th PCTL", format: "C0")
                .WithColumn<double>("milliSec", format: "F0")
                ;

            // Sweep range
            for (int expense = 100_000; expense <= 200_000; expense += 10_000)
            {
                var overrides = SimInputOverrides
                    .For<LivingExpenses>()
                    .With(x => x.FirstYearAmount, expense);

                var elapsed = Stopwatch.StartNew();
                var simResult = MultipleSimulations.RunSimulation(overrides);
                elapsed.Stop();

                var row = new List<object> { 
                    expense, 
                    simResult.SurvivalRate,
                    simResult.IterationAtPercentile(0.1).EndingBalanceReal,
                    simResult.IterationAtPercentile(0.2).EndingBalanceReal,
                    elapsed.Elapsed.TotalMilliseconds
                };
                dt.Rows.Add(row.ToArray());
            }

            // Write markdown report
            using (var writer = File.CreateText(Path.Combine(MultipleSimulations.ReportsFolder, ReportFileName)))
            {
                writer
                    .PrintMarkdownTitle2("Expense Sweep: Survival Rate vs First Year Expense")
                    .PrintMarkdownTable(dt)
                    .AppendLine();
            }
        }
    }
}
