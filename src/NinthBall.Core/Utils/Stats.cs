

namespace NinthBall.Core
{
    public static partial class Stats
    {
        /// <summary>
        /// Computes the standard deviation (volatility) of a sequence of periodic returns.
        /// </summary>
        public static double Volatility(this IEnumerable<double> returns, bool useBesselCorrection = true)
        {
            ArgumentNullException.ThrowIfNull(returns);

            double sum = 0.0, sumSq = 0.0;
            int count = 0;

            foreach (var r in returns)
            {
                sum += r;
                sumSq += r * r;
                count++;
            }

            // Adjust denominator for Bessel's correction
            var denominator =
                0 == count ? throw new ArgumentException("Input sequence must contain at least one value.", nameof(returns)) :
                1 == count && useBesselCorrection ? throw new ArgumentException("Input sequence must contain at least two values.", nameof(returns)) :
                useBesselCorrection ? count - 1 :
                count;

            return Math.Sqrt((sumSq - (sum * sum) / count) / denominator);
        }

        /// <summary>
        /// Computes the maximum drawdown from a sequence of periodic returns.
        /// </summary>
        public static double MaxDrawdown(this IEnumerable<double> returns)
        {
            ArgumentNullException.ThrowIfNull(returns);

            int count = 0;
            double peak = 1.0, value = 1.0, maxDrawdown = 0.0;

            foreach (var r in returns)
            {
                count++;

                value *= (1 + r);
                if (value > peak) peak = value;

                double drawdown = (value - peak) / peak;
                if (drawdown < maxDrawdown) maxDrawdown = drawdown;
            }

            return 0 == count 
                ? throw new ArgumentException("Input sequence must contain at least one value.", nameof(returns))
                : maxDrawdown;
        }

        /// <summary>
        /// Computes the annualized nominal return from a sequence of periodic returns.
        /// </summary>
        public static double Annualize(this IEnumerable<double> returns)
        {
            ArgumentNullException.ThrowIfNull(returns);

            double compoundReturn = 1;
            int count = 0;

            foreach (var r in returns)
            {
                checked { compoundReturn *= (1 + r); }
                count++;
            }

            return (count == 0)
                ? throw new ArgumentException("Input sequence must contain at least one value.", nameof(returns))
                : Math.Pow(compoundReturn, 1.0 / count) - 1;
        }

        /// <summary>
        /// Given a future value, inflation rate and no of years, returns inflation adjusted value in current present value.
        /// </summary>
        public static double InflationAdjustedValue(this double futureBalance, double inflationRate, int numberOfYears)
        {
            if (inflationRate < 0 || inflationRate > 1.0) throw new ArgumentException("Inflation rate must be between 0.0 and 1.0");

            double inflationFactor = Math.Pow(1 + inflationRate, numberOfYears);
            double presentValue = futureBalance / inflationFactor;
            return presentValue;
        }

        /// <summary>
        /// Calculates the first year amount (growing annually at estimatedInflation) 
        /// to reach zero balance after remainingYears 
        /// with estmated flat-growth rate.
        /// PV = W * (1 + r) * [1 - ((1 + g) / (1 + r))^n] / (r - g)
        /// </summary>
        public static double EquatedWithdrawal(double currentBalance, double estimatedROI, double estimatedInflation, int remainingYears)
        {
            // Edge cases...
            if (remainingYears <= 0) return 0.0;
            if (currentBalance <= 0) return 0.0;

            // Special case: ROI matches Inflation
            if (Math.Abs(estimatedROI - estimatedInflation) < 1e-9) return currentBalance / remainingYears;

            //................................................
            // Growing Annuity Due Formula:
            // W = PV * (r - g) / ((1 + r) * (1 - Math.Pow((1 + g) / (1 + r), n)))
            //................................................
            double r = estimatedROI;
            double g = estimatedInflation;
            int    n = remainingYears;

            double numerator   = currentBalance * (r - g);
            double denominator = (1 + r) * (1 - Math.Pow((1 + g) / (1 + r), n));
            
            return numerator / denominator;
        }
    }
}
