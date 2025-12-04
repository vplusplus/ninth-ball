
//namespace NinthBall
//{
//    /// <summary>
//    /// Builds OptimizationProblem from WhatIfConfig.
//    /// Converts YAML configuration into solver-ready optimization problem.
//    /// </summary>
//    public static class WhatIfBuilder
//    {
//        /// <summary>
//        /// Creates optimization problem from what-if configuration.
//        /// </summary>
//        public static OptimizationProblem CreateProblem(
//            WhatIfConfig config,
//            SimConfigBuilder simBuilder,
//            IReadOnlyList<ISimRating> ratings)
//        {
//            ArgumentNullException.ThrowIfNull(config);
//            ArgumentNullException.ThrowIfNull(simBuilder);
//            ArgumentNullException.ThrowIfNull(ratings);

//            if (config.Variables == null || config.Variables.Count == 0)
//                throw new FatalWarning("WhatIf configuration must define at least one variable");

//            if (ratings.Count == 0)
//                throw new FatalWarning("At least one rating must be enabled in Ratings configuration");

//            // Parse variable names to SimVariable enum
//            var variables = new List<SimVariable>();
//            var ranges = new Dictionary<SimVariable, (double, double)>();
//            var steps = new Dictionary<SimVariable, double>();

//            foreach (var (varName, varConfig) in config.Variables)
//            {
//                var simVar = ParseSimVariable(varName);
//                variables.Add(simVar);
//                ranges[simVar] = (varConfig.Min, varConfig.Max);
//                steps[simVar] = varConfig.GetEffectiveStep();
//            }

//            return new OptimizationProblem(
//                Evaluator: new SimulationEvaluator(simBuilder),
//                SearchVariables: variables,
//                SearchRanges: ranges,
//                StepSizes: steps,
//                Ratings: ratings
//            );
//        }

//        /// <summary>
//        /// Parses variable name string to SimVariable enum.
//        /// </summary>
//        private static SimVariable ParseSimVariable(string name)
//        {
//            if (Enum.TryParse<SimVariable>(name, ignoreCase: true, out var result))
//                return result;

//            throw new FatalWarning(
//                $"Unknown variable '{name}'. Valid variables: {string.Join(", ", Enum.GetNames<SimVariable>())}");
//        }
//    }
//}
