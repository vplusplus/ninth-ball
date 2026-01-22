
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;

namespace NinthBall.Core
{
    internal static class BootstrapConfiguration
    {
        public static MovingBlockBootstrapOptions GetMovingBlockBootstrapOptions() => GetAndValidateOptionalConfiguration<MovingBlockBootstrapOptions>("MovingBlockBootstrap", DefaultMovingBlockBootstrapOptions);

        public static ParametricBootstrapOptions GetParametricBootstrapOptions() => GetAndValidateOptionalConfiguration<ParametricBootstrapOptions>("ParametricBootstrap", DefaultParametricBootstrapOptions);

        static TConfig GetAndValidateOptionalConfiguration<TConfig>(string sectionName, TConfig defaultConfiguration)
        {
            // Optional configuration is read from Host configuration, not the SimInput
            // Optional configurations are invarinat across multiple runs of the simulation.
            // Host configuration is read once on process start.
            // Any additional changes are not reflected.

            var configSection = Config.Current.GetSection(sectionName);

            TConfig options = null != configSection && configSection.Exists() 
                ? configSection.Get<TConfig>() ?? throw new Exception($"Unexpected | IConfiguration returned null | {typeof(TConfig).Name} ")
                : defaultConfiguration;

            // Validate
            var context = new ValidationContext(options!, serviceProvider: null, items: null);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(options!, context, results, validateAllProperties: true);

            if (!isValid)
            {
                var csvValidationErrors = string.Join(Environment.NewLine, results.Select(x => x.ErrorMessage));
                throw new ValidationException($"{options!.GetType().Name} is invalid:{Environment.NewLine}{csvValidationErrors}");
            }

            return options!;
        }

        static MovingBlockBootstrapOptions DefaultMovingBlockBootstrapOptions => new
        (
            BlockSizes: [3, 4, 5],
            NoConsecutiveBlocks: true
        );

        static ParametricBootstrapOptions DefaultParametricBootstrapOptions => new
        (
            //# When Stocks crash (panic), investors often sell stocks and buy Bonds (safe haven). 
            //# This drives Stock prices down and Bond prices up, creating a negative correlation.
            //# While this varies by decade (it was positive in the 1980s/90s), 
            //# it has been structurally negative for most of the 21st century. 
            //# -0.15 is a prudent estimate that acknowledges this benefit without overstating it.
            StocksBondCorrelation: -0.15,

            // # Stocks tend to struggle slightly when inflation rises unexpectedly (valuation compression).
            StocksInflationCorrelation: -0.20,

            //# Bonds hate inflation (yields rise, prices fall). This linkage is weak but generally positive 
            //# in terms of yields (negative for prices). 
            //# However, for annual real returns, bonds often struggle during inflation spikes.
            //# A slight negative or near-zero correlation is safer for stress testing. 
            //# But technically, high inflation usually hurts nominal bond returns -> prices drop.
            //# Let's use a conservative negative correlation to model "bad times happen together".
            BondsInflationCorrelation: -0.10,

            Stocks: new
            (
                MeanReturn:         0.10,           // 10%
                Volatility:         0.18,           // 18%
                Skewness:           -0.5,           // Prudent Stress: Slightly negative to capture downside bias without extreme "wipeouts"
                Kurtosis:           4.0,            // Prudent Stress: "Fat tails" (Normal=3). 4.0 models a 2008-style crisis every ~25 years.
                AutoCorrelation:    0.05            // Momentum: Slight positive correlation to model market regimes.
            ),
            Bonds: new
            (
                MeanReturn:         0.05,           // 5%
                Volatility:         0.08,           // 8%
                Skewness:           0,              // Bonds are generally more symmetric in their annual return distribution.
                Kurtosis:           3,              // Normal distribution for bonds.
                AutoCorrelation:    0
            ),
            Inflation: new
            (
                MeanReturn:         0.03,           // 3%
                Volatility:         0.02,           // 2%
                Skewness:           0.5,            // Inflation shocks are often skewed to the upside
                Kurtosis:           3,
                AutoCorrelation:    0.7             // Inflation tends to be "sticky" (high autocorrelation)
            )
        );

    }
}
