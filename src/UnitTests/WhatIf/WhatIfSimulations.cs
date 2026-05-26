
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NinthBall.Core;
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace UnitTests.WhatIf
{

    [TestClass]
    public partial class WhatIfSimulations
    {
        const string ReportsFolder = @"D:\Source\ninth-ball\src\UnitTests\Reports\";
        const string WhatIfSummaryFileName = @"WhatIf-Summary.json.txt";
        const string WhatIfCashFlowFileName = @"WhatIf-CashFlow.json.txt";
        const string WhatIfDistributionFileName = @"WhatIf-Distribution.json.txt";


        [TestMethod]
        public void RunWhatIfSimulation()
        {
            // Prepare simulation configuration from /whatifinputs/*.yaml
            var baseConfig = new ConfigurationBuilder()
                .AddSimulationDefaults()
                .AddYamlResources(typeof(WhatIfSimulations).Assembly, ".WhatIfInputs.")
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

            var whatIfSummary = new
            {
                Description = ReadInstructions("WhatIf-Instructions-Summary.md"),
                Assumptions = GetCommonAssumptions(strategies),
                WhatIfOptions = whatIfOptions,
                WhatIfResults = whatIfResults,
            };

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
                    RealBalance05thPercentile = $"{pctl05.EndingBalanceReal:C0}",
                    RealBalance10thPercentile = $"{pctl10.EndingBalanceReal:C0}",
                    RealBalance20thPercentile = $"{pctl20.EndingBalanceReal:C0}",
                    RealBalance50thPercentile = $"{pctl50.EndingBalanceReal:C0}",
                },

                YearByYear05thPercentile = IterationToJsonLike(pctl05),
                YearByYear10thPercentile = IterationToJsonLike(pctl10),
                YearByYear20thPercentile = IterationToJsonLike(pctl20),
                YearByYear50thPercentile = IterationToJsonLike(pctl50),
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
                    RealBalance05thPercentile = $"{pctl05.EndingBalanceReal:C0}",
                    RealBalance10thPercentile = $"{pctl10.EndingBalanceReal:C0}",
                    RealBalance20thPercentile = $"{pctl20.EndingBalanceReal:C0}",
                    RealBalance50thPercentile = $"{pctl50.EndingBalanceReal:C0}",
                },

                RealEndingBalanceByIteration = targetResult.Iterations.AsEnumerable().Select(x => x.EndingBalanceReal.RoundToMultiples(1000.0)).ToArray(),
            };

            var formatedAndRelaxed = new JsonSerializerOptions()
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            // Write the jsonified-data
            File.WriteAllText
            (
                Path.Combine(ReportsFolder, WhatIfSummaryFileName),
                JsonSerializer.Serialize(whatIfSummary, formatedAndRelaxed)
            );

            File.WriteAllText
            (
                Path.Combine(ReportsFolder, WhatIfCashFlowFileName),
                JsonSerializer.Serialize(whatifCashFlow, formatedAndRelaxed)
            );

            File.WriteAllText
            (
                Path.Combine(ReportsFolder, WhatIfDistributionFileName),
                JsonSerializer.Serialize(whatIfDistribution, formatedAndRelaxed)
            );


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

            static object IterationToJsonLike(SimIteration iter)
            {
                return new
                {
                    Survived = iter.Success,
                    SurvivedYears = iter.SurvivedYears,

                    CashFlow = iter.ByYear.ToArray().Select(x => new 
                    {
                        x.Year,
                        x.Age,

                        JanPreTaxBalanceNominal     = x.Jan.PreTax.Amount.RoundToMultiples(100),
                        JanPostTaxBalanceNominal    = x.Jan.PostTax.Amount.RoundToMultiples(100),

                        SocialSecurityIncome        = x.Incomes.SS.RoundToMultiples(100),
                        AnnuityIncome               = x.Incomes.Ann.RoundToMultiples(100),

                        PriorYearTaxes              = x.Taxes.Total.RoundToMultiples(100),
                        CurrentYearLivingExpense    = x.Expenses.LivExp.RoundToMultiples(100),

                        WithdrawalsPreTax           = x.Withdrawals.PreTax.RoundToMultiples(100),
                        WithdrawalsPostTax          = x.Withdrawals.PostTax.RoundToMultiples(100),
                        DepositsPostTax             = x.Deposits.PostTax.RoundToMultiples(100),

                        PYEffectiveTaxRate          = ThreeDecimals(x.Taxes.TaxPCT),
                        PYEffectiveTaxRateFederal   = ThreeDecimals(x.Taxes.TaxPCTFed),
                        PYEffectiveTaxRateState     = ThreeDecimals(x.Taxes.TaxPCTState),

                        StocksROI                   = ThreeDecimals(x.ROI.StocksROI),
                        BondsROI                    = ThreeDecimals(x.ROI.BondsROI),
                        InflationRate               = ThreeDecimals(x.ROI.InflationRate),
                        ChangePreTax                = x.Change.PreTax.RoundToMultiples(100),
                        ChangePostTax               = x.Change.PostTax.RoundToMultiples(100),

                        DecPreTaxBalanceNominal     = x.Dec.PreTax.Amount.RoundToMultiples(100),
                        DecPostTaxBalanceNominal    = x.Dec.PostTax.Amount.RoundToMultiples(100),
                        DecBalanceReal              = x.DecReal.RoundToMultiples(100),

                        AnnualizedReturnNominal     = ThreeDecimals(x.RunningGrowth.AnnualizedReturn),
                        AnnualizedReturnReal        = ThreeDecimals(x.RunningGrowth.RealAnnualizedReturn),
                        RunningInflationMultiplier  = ThreeDecimals(x.InflationIndex.Consumer),

                    }).ToArray()
                };
            }

            static double ThreeDecimals(double value) => Math.Round(value, 3);
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
