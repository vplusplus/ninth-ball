

namespace NinthBall.Core
{
    internal static class SimConfig
    {
        /// <summary>
        /// How much a company pays out in dividends relative to its current stock price, expressed as a percentage.
        /// TODO: See if historical data is available, include to Bootstrapper.
        /// </summary>
        public static double TypicalStocksDividendYield => 2.0/100.0;   // 2%

        /// <summary>
        /// Interest payment made by the bond issuer, expressed as a percentage of the bond's face value 
        /// </summary>
        public static double TypicalBondCouponYield => 2.5/100.0;          // 2.5%

    }
}


