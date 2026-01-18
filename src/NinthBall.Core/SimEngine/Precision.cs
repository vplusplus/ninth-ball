namespace NinthBall.Core
{
    /// <summary>
    /// Helpers to compare or adjust double-precision-dust 
    /// </summary>
    internal static class Precision
    {
        // Amounts (Currency) - We care about cents, but simulation needs slightly more room for drift.
        public const double Amount = 0.001; 
        
        // Rates (Percentages/Ratios) - Higher precision needed for ROI and Allocations.
        public const double Rate = 1e-7;

        //......................................................................
        #region AlmostZero(), AlmostSame() and IsMoreThanZero()
        //......................................................................
        public static bool AlmostZero(this double val, double epsilon) => Math.Abs(val) < epsilon;

        public static bool AlmostSame(this double a, double b, double epsilon) => Math.Abs(a - b) < epsilon;

        public static bool IsMoreThanZero(this double val, double epsilon) => val > epsilon;

        #endregion

        //......................................................................
        #region RoundToCents() extensions
        //......................................................................

        public static Fees RoundToCents(this Fees fees) => new
        (
            PreTax: fees.PreTax.RoundToCents(),
            PostTax: fees.PostTax.RoundToCents()
        );

        public static Incomes RoundToCents(this Incomes x) => new
        (
            SS: x.SS.RoundToCents(),
            Ann: x.Ann.RoundToCents()
        );

        public static Expenses RoundToCents(this Expenses x) => new
        (
            PYTax: x.PYTax.RoundToCents(),
            LivExp: x.LivExp.RoundToCents()
        );

        public static Tax RoundToCents(this Tax x) => new
        (
            StandardDeduction: x.StandardDeduction.RoundToCents(),
            TaxOnOrdInc: x.TaxOnOrdInc.RoundToCents(),
            TaxOnDiv: x.TaxOnDiv.RoundToCents(),
            TaxOnInt: x.TaxOnInt.RoundToCents(),
            TaxOnCapGain: x.TaxOnCapGain.RoundToCents()
        );

        public static Withdrawals RoundToCents(this Withdrawals x) => new
        (
            PreTax: x.PreTax.RoundToCents(),
            PostTax: x.PostTax.RoundToCents(),
            Cash: x.Cash.RoundToCents()
        );

        public static Deposits RoundToCents(this Deposits x) => new
        (
            PostTax: x.PostTax.RoundToCents(),
            Cash: x.Cash.RoundToCents()
        );

        private static double RoundToCents(this double amount) => Math.Round(amount, 2);

        #endregion

    }
}
