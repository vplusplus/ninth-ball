namespace NinthBall.Core
{
    public static class Precision
    {
        // Amounts (Currency) - We care about cents, but simulation needs slightly more room for drift.
        public const double Amount = 0.001; 
        
        // Rates (Percentages/Ratios) - Higher precision needed for ROI and Allocations.
        public const double Rate = 1e-7;

        public static bool AlmostZero(this double val, double epsilon = Rate) => Math.Abs(val) < epsilon;
        public static bool AlmostSame(this double a, double b, double epsilon = Rate) => Math.Abs(a - b) < epsilon;
        public static bool IsMoreThanZero(this double val, double epsilon = Rate) => val > epsilon;
        
        public static double ResetNearZero(this double val, double epsilon = Rate) => Math.Abs(val) < epsilon ? 0.0 : val;
    }
}
