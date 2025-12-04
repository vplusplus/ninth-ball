
//namespace NinthBall
//{
//    /// <summary>
//    /// Evaluates simulation configurations by building configs and running simulations.
//    /// </summary>
//    public class SimulationEvaluator
//    {
//        private readonly SimConfigBuilder _builder;

//        public SimulationEvaluator(SimConfigBuilder builder)
//        {
//            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
//        }

//        /// <summary>
//        /// Evaluates a configuration with the specified variable overrides.
//        /// </summary>
//        /// <param name="overrides">Dictionary of variables to override.</param>
//        /// <returns>Simulation result.</returns>
//        public SimResult Evaluate(IReadOnlyDictionary<SimVariable, double> overrides)
//        {
//            // 1. Build config with overrides
//            var config = _builder.Build(overrides);

//            // 2. Run simulation and return result
//            return Simulation.RunSimulation(config);
//        }
//    }
//}
