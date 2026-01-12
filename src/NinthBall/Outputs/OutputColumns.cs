
namespace NinthBall.Outputs
{
    /// <summary>
    /// Column identifiers for reportable-columns
    /// </summary>
    public enum CID
    {
        NA = 0,                 // Pseudo column. Information not available
        Empty,                  // Pseudo column. Suggesting to include a divider or visual separation

        Year,                   // Year index (0-based)
        Age,                    // Completed age at the start of the year.

        JanTotal,               // Total asset value in January (start of year)
        JanValue,               // Approximate indicative value (JanTotal), less taxes and fees
        JanPreTax,              // PreTax (401k/IRA) balance in January
        JanPreTaxAlloc,         // Stock allocation % for PreTax assets in JanTotal
        JanPostTax,             // PostTax (Brokerage) balance in January
        JanPostTaxAlloc,        // Stock allocation % for PostTax assets in JanTotal
        JanCash,                // Cash balance in January

        Fees,                   // Total investment/advisory fees for the year
        TaxOrdInc,              // PYTaxes paid on ordinary income
        TaxDiv,                 // PYTaxes paid on dividends
        TaxInt,                 // PYTaxes paid on interest
        TaxCapGain,             // PYTaxes paid on capital gains
        PYTaxes,                // Total taxes paid (Sum of Ord, Div, Int, CapGains)

        LivExp,                 // Current Year Expenses (Inflation adjusted)

        SS,                     // Social Security income received
        Ann,                    // Annuity income received
        Incomes,                // Total Income (SS + Annuity)

        XPreTax,                // Net change in PreTax account (withdrawals as negative)
        XPostTax,               // Net change in PostTax account (deposits - withdrawals)
        XCash,                  // Net change in Cash account (deposits - withdrawals)

        DecTotal,               // Total Assets (PreTax + PostTax + Cash) in Dec
        DecValue,               // Approximate indicative value (Dec), less taxes and fees
        DecPreTax,              // PreTax (401k/IRA) balance in December
        DecPreTaxAlloc,         // Stock allocation % for PreTax assets in Dec
        DecPostTax,             // PostTax (Brokerage) balance in December
        DecPostTaxAlloc,        // Stock allocation % for PostTax assets in Dec
        DecCash,                // Cash balance in December

        LikeYear,               // Representative year used for historical ROI data
        ROI,                    // Effective Portfolio ROI (Blended Stock/Bond/Cash)
        AnnROI,                 // Annualized effective Portfolio ROI (Blended Stock/Bond/Cash)
        ROIAmount,              // Total changes contributed by ROI gorwth or losss
        ROIStocks,              // Return on Stocks for the year (PCT)
        ROIBonds,               // Return on Bonds for the year (PCT)
        ROICash,                // Return on Cash for the year (PCT)
        InflationRate           // Consumer Price Index (CPI-U)
    }
}
