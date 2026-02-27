using Microsoft.Extensions.Configuration;
using NinthBall.Core;
using NinthBall.Reports.PrettyPrint;
using System.Data;
using System.Diagnostics;

namespace UnitTests.WhatIf
{
    public partial class MultipleSimulations
    {
        [TestMethod]
        public async Task FindMaxFirstYearExpense()
        {
            const string ReportFileName = "MaxFirstYearExpense.md";
            const double MinExpense = 100000;
            const double MaxExpense = 200000;
            const double Steps = 10000;

            // Base configuration (same as MultipleSimulations)
            var baseConfig = new ConfigurationBuilder()
                .AddSimulationDefaults()
                .AddYamlResources(typeof(MultipleSimulations).Assembly, ".TestInputs.")
                .Build();

            // Read required sections (only for reporting)
            var p = baseConfig.ReadAndValidateRequiredSestion<SimParams>();
            var init = baseConfig.ReadAndValidateRequiredSestion<Initial>();
            var exp = baseConfig.ReadAndValidateRequiredSestion<LivingExpenses>();

            // Prepare output table
            var dt = new DataTable()
                .WithColumn<double>("Y0-Expense",       format: "C0")
                .WithColumn<double>("SurvivalRate",     format: "P0")
                .WithColumn<double>("Balance(r) 10th",  format: "C0")
                .WithColumn<double>("Balance(r) 20th",  format: "C0")
                .WithColumn<double>("milliSec",         format: "F0")
                ;

            // Sweep range
            for (double expense = MinExpense; expense <= MaxExpense; expense += Steps)
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

            // Order by descenging 
            // dt.DefaultView.Sort = "Y0-Expense DESC";
            // dt = dt.DefaultView.ToTable();

            var keyInputs = new
            {
                From = $"{p.StartAge} years",
                To = $"{p.StartAge + p.NoOfYears} years",
                PreTax = $"{init.PreTax.Amount / 1000000:C1} M",
                PostTax = $"{init.PostTax.Amount / 1000000:C1} M",
            };

            // Write markdown report
            using (var writer = File.CreateText(Path.Combine(MultipleSimulations.ReportsFolder, ReportFileName)))
            {
                writer
                    .PrintMarkdownTitle2("Frst year expense vs survival rate")

                    .PrintMarkdownTitle3("Input")
                    .PrintMarkdownRecordWide(keyInputs)

                    .PrintMarkdownTitle3("Results")
                    .PrintMarkdownTable(dt)
                    .AppendLine();
            }
        }
    }
}
