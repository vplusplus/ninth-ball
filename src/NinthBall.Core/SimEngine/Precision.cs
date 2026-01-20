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
        #region Precision aware comparison: AlmostZero(), AlmostSame() and IsMoreThanZero()
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
            LivExp: x.LivExp.RoundToCents()
        );

        public static Taxes RoundToCents(this Taxes x) => new
        (
            GrossIncome: x.GrossIncome.RoundToCents(),
            Federal: x.Federal.RoundToCents(),
            State: x.State.RoundToCents()
        );

        public static Taxes.Inc RoundToCents(this Taxes.Inc x) => new
        (
            OrdInc: x.OrdInc.RoundToCents(),
            DIV: x.DIV.RoundToCents(),
            INT: x.INT.RoundToCents(),
            LTCG: x.LTCG.RoundToCents()
        );

        public static Taxes.TD RoundToCents(this Taxes.TD x) => new
        (
            Deduction: x.Deduction.RoundToCents(),
            Taxable: x.Taxable.RoundToCents(),
            MarginalRate: x.MarginalRate,
            Tax: x.Tax.RoundToCents(),
            TaxBreakdown: x.TaxBreakdown.RoundToCents()
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
