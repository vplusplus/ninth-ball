
namespace NinthBall.Outputs
{
    /// <summary>
    /// Column identifiers for reportable-columns
    /// </summary>
    internal enum CID
    {
        NA,                     // Pseudo column. Information not available
        Empty,                  // Pseudo column. Suggesting to include a divider or visual separation

        Year,                   // Year index (0-based)
        Age,                    // Primary person's age at year end

        JanTotal,               // Total asset value in January (start of year)
        JanValue,               // Approximate indicative value (JanTotal), less taxes and fees
        JanPreTax,              // PreTax (401k/IRA) balance in January
        JanPreTaxAlloc,         // Stock allocation % for PreTax assets in JanTotal
        JanPostTax,             // PostTax (Brokerage) balance in January
        JanPostTaxAlloc,        // Stock allocation % for PostTax assets in JanTotal
        JanCash,                // Cash balance in January

        Fees,                   // Total investment/advisory fees for the year
        TaxOrdIncome,           // PYTaxes paid on ordinary income
        TaxDiv,                 // PYTaxes paid on dividends
        TaxInt,                 // PYTaxes paid on interest
        TaxCapGains,            // PYTaxes paid on capital gains
        PYTaxes,                    // Total taxes paid (Sum of Ord, Div, Int, CapGains)

        CYExp,                  // Current Year Expenses (Inflation adjusted)

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
        Change,                 // Total changes contributed for ROI gorwth or losss
        ROI,                    // Effective Portfolio ROI (Blended Stock/Bond/Cash)
        ROIStocks,              // Return on Stocks for the year
        ROIBonds,               // Return on Bonds for the year
        ROICash,                // Return on Cash for the year
    }
}
