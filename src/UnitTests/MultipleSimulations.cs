using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NinthBall.Core;
using NinthBall.Reports;

namespace UnitTests
{
    [TestClass]
    public class MultipleSimulations
    {
        [TestMethod]
        public async Task RunTestSimulations()
        {
            var now = DateTime.Now;
            var baseDir = @$"D:/Junk/SimReports-{now:MMdd-HHmmss}/";

            if (!Directory.Exists(baseDir)) Directory.CreateDirectory(baseDir);

            string[] growthObjectives = ["FlatGrowth", "HistoricalGrowth", "RandomHistoricalGrowth", "ExpectedGrowth", "ConservativeGrowth", "HighRiskGrowth"];

            var cfg = new ConfigurationBuilder()
                .AddSimulationDefaults()
                .AddReportDefaults()
                .AddYamlResources(typeof(MultipleSimulations).Assembly, ".TestInputs.")
                .Build();

            foreach(var growthObjective in growthObjectives)
            {
                var htmlFileName = Path.Combine(baseDir, $"{growthObjective}.html");

                var oldParams = cfg.GetSection("SimParams").Get<SimParams>();
                string[] newObjectives = [.. oldParams!.Objectives, growthObjective];
                var newParams = oldParams! with
                {
                    Objectives = newObjectives
                };

                var oldOutput = cfg.GetSection("Outputs").Get<OutputOptions>();
                var newOutput = oldOutput! with
                {
                    Html = oldOutput.Html with
                    {
                        File = htmlFileName
                    }
                };

                await RunSimulation(newParams, newOutput);
            }
        }

        private static async Task RunSimulation(params object[] overrides)
        {
            // We create and destroy DI container for each simulation session.
            var simSessionBuilder = Host.CreateEmptyApplicationBuilder(settings: new());

            // Apply default choices and user overrides.
            simSessionBuilder.Configuration
                .AddSimulationDefaults()
                .AddReportDefaults()
                .AddYamlResources(typeof(MultipleSimulations).Assembly, ".TestInputs.")
                ;

            // Assemble simulation and reporting components.
            simSessionBuilder.Services
                .AddSimulationComponents()
                .AddReportComponents()
                ;

            // Apply overrides
            foreach (var obj in overrides) if (null != obj) simSessionBuilder.Services.AddSingleton(obj.GetType(), obj);

            // Run simulation, export reports
            using (var simSession = simSessionBuilder.Build())
            {
                // Resolve required services
                ISimulation simulation = simSession.Services.GetRequiredService<ISimulation>();
                ISimulationReports simReports = simSession.Services.GetRequiredService<ISimulationReports>();

                var simResults = simulation.Run();
                await simReports.GenerateAsync(simResults);

                var sr = simResults.SurvivalRate;
                var txtSurvivalRate = sr > 0.99 ? $"{sr:P1}" : $"{sr:P0}";
                Console.WriteLine($" Survival rate: {txtSurvivalRate}");
            }
        }
    }
}
