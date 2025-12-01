
namespace NinthBall.Examples
{
    /// <summary>
    /// Examples demonstrating how to use ratings and optimization.
    /// </summary>
    public static class OptimizationExamples
    {
        /// <summary>
        /// Example 1: Single-rating optimization.
        /// Find the minimum starting balance needed to achieve 98% survival rate.
        /// </summary>
        public static void Example_SingleRating_MinimumBalance()
        {
            // 1. Create config builder with base configuration
            var builder = new SimConfigBuilder("path/to/base-config.yaml");

            // 2. Create evaluator
            var evaluator = new SimulationEvaluator(builder);

            // 3. Define optimization problem
            var problem = new OptimizationProblem(
                Evaluator: evaluator,
                
                // Search over starting balance only
                SearchVariables: new[] { SimVariable.StartingBalance },
                
                SearchRanges: new Dictionary<SimVariable, (double, double)>
                {
                    [SimVariable.StartingBalance] = (100_000, 2_000_000)
                },
                
                StepSizes: new Dictionary<SimVariable, double>
                {
                    [SimVariable.StartingBalance] = 100_000
                },
                
                // Ratings: Minimize capital with 98% survival constraint
                Ratings: new ISimRating[]
                {
                    new SurvivalRateRating(new SurvivalRateConfig { MinRequired = 0.98 }),
                    new CapitalRequirementRating(new CapitalRequirementConfig())
                }
            );

            // 4. Solve
            var result = UnifiedSolver.Solve(problem);

            // 5. Get best solution
            var best = result.BestSolution;
            
            Console.WriteLine($"Minimum starting balance: {best.Inputs[SimVariable.StartingBalance]:C0}");
            Console.WriteLine($"Achieved survival rate: {best.Result.SurvivalRate:P1}");
            Console.WriteLine();
            Console.WriteLine("Ratings (0.0 = unacceptable, 1.0 = ideal):");
            foreach (var (ratingName, score) in best.Result.Scores)
            {
                Console.WriteLine($"  {ratingName}: {score}");  // Score.ToString() formats as percentage
            }
            Console.WriteLine($"\nTotal evaluations: {result.TotalEvaluations}");
        }

        /// <summary>
        /// Example 2: Multi-rating optimization.
        /// Explore trade-offs between survival rate, withdrawal rate, and capital.
        /// </summary>
        public static void Example_MultiRating_TradeOffs()
        {
            var builder = new SimConfigBuilder("path/to/base-config.yaml");
            var evaluator = new SimulationEvaluator(builder);

            var problem = new OptimizationProblem(
                Evaluator: evaluator,
                
                // Search over multiple variables
                SearchVariables: new[] 
                { 
                    SimVariable.StartingBalance,
                    SimVariable.WithdrawalRate 
                },
                
                SearchRanges: new Dictionary<SimVariable, (double, double)>
                {
                    [SimVariable.StartingBalance] = (500_000, 2_000_000),
                    [SimVariable.WithdrawalRate] = (0.02, 0.05)
                },
                
                StepSizes: new Dictionary<SimVariable, double>
                {
                    [SimVariable.StartingBalance] = 100_000,
                    [SimVariable.WithdrawalRate] = 0.005
                },
                
                // Multiple competing ratings
                Ratings: new ISimRating[]
                {
                    new SurvivalRateRating(new SurvivalRateConfig { MinRequired = 0.95 }),
                    new WithdrawalRateRating(new WithdrawalRateConfig()),
                    new CapitalRequirementRating(new CapitalRequirementConfig())
                }
            );

            var result = UnifiedSolver.Solve(problem);

            // Explore Pareto front with absolute scores
            Console.WriteLine($"Found {result.ParetoFront.Count} Pareto-optimal solutions:\n");
            Console.WriteLine($"{"Balance",-12} {"Withdrawal",-12} {"Survival",-10} | Rating Scores (0-1)");
            Console.WriteLine($"{"",-12} {"",-12} {"",-10} | Sur   Wdr   Cap");
            Console.WriteLine(new string('-', 75));
            
            foreach (var solution in result.ParetoFront.OrderByDescending(s => s.Result.SurvivalRate))
            {
                var withdrawalObj = solution.Result.Objectives.OfType<PCTWithdrawalObjective>().Single();
                var survivalRating = solution.Result.Scores.Values.First();
                var withdrawalRating = solution.Result.Scores.Values.Skip(1).First();
                var capitalRating = solution.Result.Scores.Values.Skip(2).First();
                
                Console.WriteLine(
                    $"{solution.Inputs[SimVariable.StartingBalance],-12:C0} " +
                    $"{withdrawalObj.FirstYearPct,-12:P2} " +
                    $"{solution.Result.SurvivalRate,-10:P1} | " +
                    $"{survivalRating,4:P0}  " +
                    $"{withdrawalRating,4:P0}  " +
                    $"{capitalRating,4:P0}");
            }
            
            Console.WriteLine("\nInterpretation:");
            Console.WriteLine("- Scores are absolute: 1.0 = ideal based on domain knowledge");
            Console.WriteLine("- Scores are stable - same solution scores same regardless of dataset");
        }

        /// <summary>
        /// Example 3: Independent rating evaluation without optimization.
        /// Demonstrates that ratings work independently for quality assessment.
        /// </summary>
        public static void Example_IndependentRating()
        {
            var builder = new SimConfigBuilder("path/to/base-config.yaml");
            
            // Define what we want to rate (no optimization, just assessment)
            var ratings = new ISimRating[]
            {
                new SurvivalRateRating(new SurvivalRateConfig { MinRequired = 0.95 }),
                new MedianBalanceRating(new MedianBalanceConfig()),
                new CapitalRequirementRating(new CapitalRequirementConfig())
            };
            
            // Build specific config and run simulation
            var overrides = new Dictionary<SimVariable, double>
            {
                [SimVariable.StartingBalance] = 1_200_000,
                [SimVariable.WithdrawalRate] = 0.04
            };
            
            var config = builder.Build(overrides);
            var result = Simulation.RunSimulation(config);
            
            // Rate the simulation
            Console.WriteLine("Rating single simulation:");
            Console.WriteLine($"  Starting Balance: {overrides[SimVariable.StartingBalance]:C0}");
            Console.WriteLine($"  Withdrawal Rate: {overrides[SimVariable.WithdrawalRate]:P2}");
            Console.WriteLine();
            Console.WriteLine("Rating Scores:");
            
            foreach (var rating in ratings)
            {
                Score score = rating.Score(result);
                string assessment = score.Value switch
                {
                    >= 8.0 => "Excellent",
                    >= 6.0 => "Good",
                    >= 4.0 => "Acceptable",
                    >= 2.0 => "Poor",
                    _ => "Unacceptable"
                };
                
                Console.WriteLine($"  {rating.Name}: {score} ({assessment})");
            }
        }
    }
}
