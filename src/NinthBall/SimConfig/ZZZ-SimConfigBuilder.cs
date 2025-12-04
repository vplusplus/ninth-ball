//using System.Collections.Concurrent;

//namespace NinthBall
//{
//    /// <summary>
//    /// Enumeration of all optimizable variables in the simulation configuration.
//    /// </summary>
//    public enum SimVariable
//    {
//        StartingBalance,
//        StockAllocation,
//        MaxDrift,
//        WithdrawalRate,
//        WithdrawalIncrement,
//        BufferAmount,
//        AnnualFees
//    }

//    /// <summary>
//    /// Builder for creating modified SimConfig instances from a base configuration file.
//    /// Uses a lazy-loading pattern for the base config and a dynamic mutation system.
//    /// </summary>
//    public class SimConfigBuilder
//    {
//        private readonly Lazy<SimConfig> _baseConfig;
        
//        // Map of Enum -> Mutation Action
//        private static readonly Dictionary<SimVariable, Func<SimConfig, double, SimConfig>> _mutators = 
//            new()
//            {
//                [SimVariable.StartingBalance] = SimConfigMutations.WithStartingBalance,
//                [SimVariable.StockAllocation] = SimConfigMutations.WithStockAllocation,
//                [SimVariable.MaxDrift] = SimConfigMutations.WithMaxDrift,
//                [SimVariable.WithdrawalRate] = SimConfigMutations.WithWithdrawalRate,
//                [SimVariable.WithdrawalIncrement] = SimConfigMutations.WithWithdrawalIncrement,
//                [SimVariable.BufferAmount] = SimConfigMutations.WithBufferAmount,
//                [SimVariable.AnnualFees] = SimConfigMutations.WithAnnualFees
//            };

//        /// <summary>
//        /// Initializes a new instance of the SimConfigBuilder.
//        /// </summary>
//        /// <param name="inputFileName">Path to the YAML configuration file.</param>
//        public SimConfigBuilder(string inputFileName)
//        {
//            // Lazy initialization ensures we only read/parse the file once, and only if needed.
//            // Thread-safe by default.
//            _baseConfig = new Lazy<SimConfig>(() => SimConfigReader.Read(inputFileName));
//        }

//        /// <summary>
//        /// Gets the immutable base configuration loaded from the file.
//        /// </summary>
//        public SimConfig BaseConfig => _baseConfig.Value;

//        /// <summary>
//        /// Creates a new SimConfig by applying the specified overrides to the base configuration.
//        /// </summary>
//        /// <param name="overrides">Dictionary of variables to modify and their new values.</param>
//        /// <returns>A new SimConfig instance with the applied changes.</returns>
//        public SimConfig Build(IReadOnlyDictionary<SimVariable, double> overrides)
//        {
//            var currentConfig = BaseConfig;

//            if (overrides == null || overrides.Count == 0)
//            {
//                return currentConfig;
//            }

//            foreach (var (variable, value) in overrides)
//            {
//                if (_mutators.TryGetValue(variable, out var mutator))
//                {
//                    try
//                    {
//                        currentConfig = mutator(currentConfig, value);
//                    }
//                    catch (Exception ex)
//                    {
//                        throw new InvalidOperationException(
//                            $"Failed to apply mutation for '{variable}' with value {value}. " +
//                            $"This may indicate a null or incomplete configuration.", ex);
//                    }
//                }
//                else
//                {
//                    // Should not happen if Enum is exhaustive in dictionary, 
//                    // but good to be explicit or just ignore.
//                    throw new NotImplementedException($"Mutator for {variable} is not implemented.");
//                }
//            }

//            return currentConfig;
//        }
//    }
//}
