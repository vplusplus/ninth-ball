
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NinthBall.Core;
using NinthBall.Reports.PrettyPrint;
using System.Data;
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace UnitTests.WhatIf
{

    [TestClass]
    public partial class WhatIfSimulations
    {
        static string WhatIfInoutFolder => MyConfig.Instance["In"] ?? throw new Exception("Missing config entry: 'In'");
        static string WhatIfReportsFolder => MyConfig.Instance["Out"] ?? throw new Exception("Missing config entry: 'Out'");

        const string WhatIfCashFlowFileName = @"WhatIf-CashFlow.json.txt";
        const string WhatIfDistributionFileName = @"WhatIf-Distribution.json.txt";
        const string WhatIfSurvivalMatrixFileName = @"WhatIf-SurvivalMatrix.md";

        [TestMethod]
        public void RunWhatIfSimulation()
        {
            Console.WriteLine($"IN:  {WhatIfInoutFolder}");
            Console.WriteLine($"OUT: {WhatIfReportsFolder}");

            // Prepare simulation configuration from /whatifinputs/*.yaml
            var baseConfig = new ConfigurationBuilder()
                .AddSimulationDefaults()
                //.AddYamlResourcesFromAssembly(typeof(WhatIfSimulations).Assembly, ".WhatIfInputs.")
                .AddYamlResourcesMatchingGlobPattern(WhatIfInoutFolder)
                .Build();

            // Prepare a combinations of initial balance, first year exp and start age combinations.
            var baseParams = baseConfig.ReadAndValidateRequiredSection<SimParams>();
            var whatIfOptions = baseConfig.ReadAndValidateRequiredSection<WhatIfOptions>();
            var varyBy = new List<WhatIfVariant>();
            for (double ib = whatIfOptions.InitialBalance.Min; ib <= whatIfOptions.InitialBalance.Max; ib += whatIfOptions.InitialBalance.Steps)
                for (double y0Exp = whatIfOptions.FirstYearExpense.Min; y0Exp <= whatIfOptions.FirstYearExpense.Max; y0Exp += whatIfOptions.FirstYearExpense.Steps)
                    for (int startAge = (int)whatIfOptions.StartAge.Min; startAge <= whatIfOptions.StartAge.Max; startAge += (int)whatIfOptions.StartAge.Steps)
                    {
                        var adjustedNumYears = baseParams.NoOfYears - (startAge - baseParams.StartAge);
                        varyBy.Add(new WhatIfVariant(ib, y0Exp, startAge, adjustedNumYears));
                    }
            Console.WriteLine($"Will test {varyBy.Count} combinations of initial-balance, year-0-expense and start-age.");

            // Run multiple simulations for what-if variants in parallel, collect results.
            var elapsed = Stopwatch.StartNew();
            var strategies = new HashSet<string>();
            var whatIfResults = varyBy
                .AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .Select(x => TryOneVariation(baseConfig, x, strategies))
                .ToList();
            elapsed.Stop();
            Console.WriteLine($"Completed {whatIfResults.Count:#,0} simulations | Elapsed: {elapsed.Elapsed.TotalSeconds:#,0} sec");

            // Run a single simulation for the chosen target scenario.
            WhatIfVariant targetInput = new(
                whatIfOptions.InitialBalance.Target,
                whatIfOptions.FirstYearExpense.Target,
                (int)whatIfOptions.StartAge.Target,
                baseParams.NoOfYears - ((int)whatIfOptions.StartAge.Target - baseParams.StartAge)
            );
            SimResult targetResult = RunOneSimulation(baseConfig, targetInput);

            //................................................
            // Generate output structures, to be json-ified.
            //................................................
            var pctl05 = targetResult.IterationAtPercentile(0.05);
            var pctl10 = targetResult.IterationAtPercentile(0.10);
            var pctl20 = targetResult.IterationAtPercentile(0.20);
            var pctl50 = targetResult.IterationAtPercentile(0.50);

            var whatifCashFlow = new
            {
                Description = ReadInstructions("WhatIf-Instructions-CashFlow.md"),

                Input =  new 
                {
                    targetInput.InitialBalance,
                    targetInput.FirstYearExpense,
                    targetInput.StartAge,
                    targetInput.NoOfYears,
                    NoOfIterations = targetResult.Iterations.Count(),
                },

                Assumptions = targetResult.Strategies.ToArray(),

                ResultSummary = new 
                {
                    SurvivalRate = Math.Round(targetResult.SurvivalRate, 2),
                    RealBalance05thPercentile = $"{pctl05.RealEndingBalance:C0}",
                    RealBalance10thPercentile = $"{pctl10.RealEndingBalance:C0}",
                    RealBalance20thPercentile = $"{pctl20.RealEndingBalance:C0}",
                    RealBalance50thPercentile = $"{pctl50.RealEndingBalance:C0}",
                },

                //YearByYear05thPercentile = IterationToJsonLike(pctl05),
                //YearByYear10thPercentile = IterationToJsonLike(pctl10),
                //YearByYear20thPercentile = IterationToJsonLike(pctl20),
                //YearByYear50thPercentile = IterationToJsonLike(pctl50),

                YearByYear05thPercentile = pctl05,
                YearByYear10thPercentile = pctl10,
                YearByYear20thPercentile = pctl20,
                YearByYear50thPercentile = pctl50,

            };

            var whatIfDistribution = new
            {
                Description = ReadInstructions("WhatIf-Instructions-Distribution.md"),

                Input = new
                {
                    targetInput.InitialBalance,
                    targetInput.FirstYearExpense,
                    targetInput.StartAge,
                    targetInput.NoOfYears,
                    NoOfIterations = targetResult.Iterations.Count(),
                },

                Assumptions = targetResult.Strategies.ToArray(),

                ResultSummary = new
                {
                    SurvivalRate = Math.Round(targetResult.SurvivalRate, 2),
                    RealBalance05thPercentile = $"{pctl05.RealEndingBalance:C0}",
                    RealBalance10thPercentile = $"{pctl10.RealEndingBalance:C0}",
                    RealBalance20thPercentile = $"{pctl20.RealEndingBalance:C0}",
                    RealBalance50thPercentile = $"{pctl50.RealEndingBalance:C0}",
                },

                RealEndingBalanceByIteration = targetResult.Iterations.AsEnumerable().Select(x => x.RealEndingBalance.RoundToMultiples(1000.0)).ToArray(),
            };

            var formatedAndRelaxed = new JsonSerializerOptions()
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals
            };

            // Ensure the output folder exists
            if (!Directory.Exists(WhatIfReportsFolder)) Directory.CreateDirectory(WhatIfReportsFolder);

            // Survival matrix (md)
            using (var writer = File.CreateText(Path.Combine(WhatIfReportsFolder, WhatIfSurvivalMatrixFileName)))
            {
                RenderSurvivalMatrixReport(
                    writer,
                    GetCommonAssumptions(strategies),
                    whatIfResults,
                    whatIfOptions.TargetSurvivalRate
                );
            }

            // Write distribution of real-ending-balance (to see the skew)
            File.WriteAllText
            (
                Path.Combine(WhatIfReportsFolder, WhatIfDistributionFileName),
                JsonSerializer.Serialize(whatIfDistribution, formatedAndRelaxed)
            );

            // Write year-by-year on select percentiles to review the cashflow.
            File.WriteAllText
            (
                Path.Combine(WhatIfReportsFolder, WhatIfCashFlowFileName),
                JsonSerializer.Serialize(whatifCashFlow, formatedAndRelaxed)
            );

            Console.WriteLine($"See '{WhatIfReportsFolder}' for results.");

            //................................................
            // Helpers
            //................................................
            static string[] GetCommonAssumptions(IEnumerable<string> strategies)
            {
                // Fragile logic.
                // For now, only one component (Living expenses) is scenario specific
                return strategies
                    .Where(x => !x.Contains("Living expenses", StringComparison.OrdinalIgnoreCase))
                    .ToArray();
            }

        }

        static WhatIfResult TryOneVariation(IConfiguration baseConfiguration, WhatIfVariant varyBy, HashSet<string> strategies)
        {
            var simResult = RunOneSimulation(baseConfiguration, varyBy);

            lock (strategies)
            {
                foreach (var strategy in simResult.Strategies) strategies.Add(strategy);
            }

            return new(simResult);
        }

        static SimResult RunOneSimulation(IConfiguration baseConfiguration, WhatIfVariant varyBy)
        {
            var overrides = SimInputOverrides
                .For<SimParams>()
                    .With(x => x.StartAge, varyBy.StartAge)
                    .With(x => x.NoOfYears, varyBy.NoOfYears)
                .For<Initial>()
                    .With(x => x.PreTax.Amount, varyBy.InitialBalance / 2)
                    .With(x => x.PostTax.Amount, varyBy.InitialBalance / 2)
                .For<LivingExpenses>()
                    .With(x => x.FirstYearAmount, varyBy.FirstYearExpense);

            var builder = Host.CreateEmptyApplicationBuilder(settings: new());

            builder.Configuration
                .AddConfiguration(baseConfiguration)
                .AddOverrides(overrides);

            builder.Services
                .AddSimulationComponents();

            using (var session = builder.Build())
            {
                var simResult = session.Services.GetRequiredService<ISimulation>().Run();
                return simResult;
            }
        }

        static void RenderSurvivalMatrixReport(TextWriter writer, string[] commonAssumptions, List<WhatIfResult> whatIfResults, double targetSurvivalRate)
        {
            writer.WriteLine($"# WhatIf: Survival rates");
            writer.WriteLine($"");

            writer.WriteLine($"## Common assumptions");
            writer.WriteLine($"");
            foreach (var assumption in commonAssumptions) writer.WriteLine($"* {assumption}");
            writer.WriteLine($"");

            writer.WriteLine($"## Survival matrix:");
            writer.WriteLine($"");

            var initialBalances = whatIfResults.Select(x => x.InitialBalance).Distinct().OrderBy(x => x).ToArray();
            var year0Expenses = whatIfResults.Select(x => x.Year0Expense).Distinct().OrderBy(x => x).ToArray();
            var startAges = whatIfResults.Select(x => (x.StartAge, x.AgeRange)).Distinct().OrderBy(x => x).ToArray();

            foreach (var initialBalance in initialBalances)
            {
                var pivot = whatIfResults
                    .Where(x => x.InitialBalance == initialBalance)
                    .ToDictionary(
                        keySelector: x => (x.StartAge, x.Year0Expense),
                        elementSelector: x => x.SurvivalRate
                    );

                DataTable dt = new DataTable();

                // Table row
                dt.WithColumn("Start at", typeof(string));
                foreach (var y0exp in year0Expenses) dt.WithColumn($"{y0exp/1000:C0} K", typeof(string));

                foreach(var startAge in startAges)
                {
                    var row = new List<string> { $"{startAge.AgeRange}" };

                    foreach(var y0Exp in year0Expenses)
                    {
                        var found = pivot.TryGetValue((startAge.StartAge, y0Exp), out var rate) && rate >= targetSurvivalRate;
                        var cell = found ? $"{rate:P0}" : string.Empty;
                        row.Add(cell);
                    }

                    dt.AppendRow(row.ToArray());
                }

                writer.WriteLine($"### Initial balance: {initialBalance/1000000:C1} M");
                writer.WriteLine($"");
                writer.PrintMarkdownTable(dt);
                writer.WriteLine($"");
            }
        }

        private static string ReadInstructions(string resourceNameEndsWith)
        {
            var assmbly = typeof(WhatIfSimulations).Assembly;
            var resName = assmbly.GetManifestResourceNames().Where(x => x.EndsWith(resourceNameEndsWith, StringComparison.OrdinalIgnoreCase)).Single();
            using var resStream = assmbly.GetManifestResourceStream(resName);
            using var reader = new StreamReader(resStream!);
            return reader.ReadToEnd();
        }
    }
}
