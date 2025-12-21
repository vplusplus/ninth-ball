
namespace NinthBall
{
    internal static class MathUtils
    {
        /// <summary>
        /// Acklam's approximation for the inverse normal cumulative distribution function.
        /// Converts a probability (0, 1) to a standard normal deviate (Z-score).
        /// </summary>
        public static double InverseNormalCDF(double p)
        {
            if (p <= 0 || p >= 1) throw new ArgumentOutOfRangeException(nameof(p), "Probability must be between 0 and 1 exclusive.");

            // Coefficients for low, middle, and high regions
            const double a1 = -3.969683028665376e+01;
            const double a2 =  2.209460984245205e+02;
            const double a3 = -2.759285104469687e+02;
            const double a4 =  1.383577518672690e+02;
            const double a5 = -3.066479806614716e+01;
            const double a6 =  2.506628277459239e+00;

            const double b1 = -5.447609879822406e+01;
            const double b2 =  1.615858368580409e+02;
            const double b3 = -1.556989798598866e+02;
            const double b4 =  6.680131188771972e+01;
            const double b5 = -1.328068155288572e+01;

            const double c1 = -7.784894002430293e-03;
            const double c2 = -3.223964580411365e-01;
            const double c3 = -2.400758277161838e+00;
            const double c4 = -2.549732539343734e+00;
            const double c5 =  4.374664141464968e+00;
            const double c6 =  2.938163982698783e+00;

            const double d1 =  7.784695709041462e-03;
            const double d2 =  3.224671290700398e-01;
            const double d3 =  2.445134137142996e+00;
            const double d4 =  3.754408661907416e+00;

            const double p_low = 0.02425;
            const double p_high = 1 - p_low;

            double x, q, r;

            if (p < p_low)
            {
                q = Math.Sqrt(-2 * Math.Log(p));
                x = (((((c1 * q + c2) * q + c3) * q + c4) * q + c5) * q + c6) /
                    ((((d1 * q + d2) * q + d3) * q + d4) * q + 1);
            }
            else if (p <= p_high)
            {
                q = p - 0.5;
                r = q * q;
                x = (((((a1 * r + a2) * r + a3) * r + a4) * r + a5) * r + a6) * q /
                    (((((b1 * r + b2) * r + b3) * r + b4) * r + b5) * r + 1);
            }
            else
            {
                q = Math.Sqrt(-2 * Math.Log(1 - p));
                x = -(((((c1 * q + c2) * q + c3) * q + c4) * q + c5) * q + c6) /
                     ((((d1 * q + d2) * q + d3) * q + d4) * q + 1);
            }

            return x;
        }

        /// <summary>
        /// Performs a 2x2 Cholesky correlation to link two independent normal variables.
        /// Given independent Z1, Z2 and correlation rho, returns (X1, X2) where Corr(X1, X2) = rho.
        /// </summary>
        public static (double X1, double X2) Correlate(double z1, double z2, double rho)
        {
            // L = [ 1          0 ]
            //     [ rho  sqrt(1-rho^2) ]
            double x1 = z1;
            double x2 = rho * z1 + Math.Sqrt(1 - rho * rho) * z2;
            return (x1, x2);
        }

        /// <summary>
        /// Cornish-Fisher expansion to adjust a standard normal deviate for skewness and kurtosis.
        /// </summary>
        public static double CornishFisher(double z, double skew, double kurtosis)
        {
            double s = skew;
            double k = kurtosis - 3; // Excess kurtosis

            // Cornish-Fisher expansion (4th order)
            return z 
                + (z * z - 1) * s / 6 
                + (z * z * z - 3 * z) * k / 24 
                - (2 * z * z * z - 5 * z) * s * s / 36;
        }
    }
}
