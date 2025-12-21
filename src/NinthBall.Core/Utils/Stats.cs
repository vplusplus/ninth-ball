

namespace NinthBall
{
    public static partial class Stats
    {
        public static bool AlmostZero(this double number) => Math.Abs(number) <= 1e-6;


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

        public static double RoundX(this double value, int roundTo) => Math.Round(value / roundTo) * roundTo;
        public static double Round10(this double value) => RoundX(value, 10);
        public static double Round100(this double value) => RoundX(value, 100);
        public static double Round1000(this double value) => RoundX(value, 1000);
        public static string Thousands(this double value, int decimalPlaces = 2) => $"{(value / 1000).ToString($"C{decimalPlaces}")} K";
        public static string Millions(this double value, int decimalPlaces = 1) => $"{(value / 1000000).ToString($"C{decimalPlaces}")} M";

        /// <summary>
        /// Calculates the annual withdrawal amount (Annuity Due) for a growing annuity that depletes a balance to zero.
        /// PV = W * (1 + r) * [1 - ((1 + g) / (1 + r))^n] / (r - g)
        /// </summary>
        /// <param name="currentBalance">Present value of the asset pool.</param>
        /// <param name="estimatedROI">Expected annual return (nominal).</param>
        /// <param name="estimatedInflation">Expected annual increment (inflation).</param>
        /// <param name="remainingYears">Number of years left (including current).</param>
        /// <returns>The withdrawal amount for the current year.</returns>
        public static double EquatedWithdrawal(double currentBalance, double estimatedROI, double estimatedInflation, int remainingYears)
        {
            if (remainingYears <= 0) return 0.0;
            if (currentBalance <= 0) return 0.0;

            // Special case: ROI matches Inflation
            if (Math.Abs(estimatedROI - estimatedInflation) < 1e-9)
            {
                return currentBalance / remainingYears;
            }

            double r = estimatedROI;
            double g = estimatedInflation;
            int n = remainingYears;

            // Growing Annuity Due Formula:
            // W = PV * (r - g) / ((1 + r) * (1 - Math.Pow((1 + g) / (1 + r), n)))
            
            double numerator = currentBalance * (r - g);
            double denominator = (1 + r) * (1 - Math.Pow((1 + g) / (1 + r), n));

            return numerator / denominator;
        }

        public static (double, double) Swap(double first, double second)
        {
            var temp = first; first = second; second = temp;
            return (first, second);
        }
    }
}
