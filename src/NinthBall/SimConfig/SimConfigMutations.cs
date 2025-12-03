namespace NinthBall
{
    /// <summary>
    /// Extension methods for modifying SimConfig properties.
    /// </summary>
    public static class SimConfigMutations
    {
        public static SimConfig WithStartingBalance(this SimConfig config, double value) 
            => config with { InitialBalance = value };

        public static SimConfig WithStockAllocation(this SimConfig config, double value) 
            => config with { StockAllocation = value };

        public static SimConfig WithMaxDrift(this SimConfig config, double value) 
            => config with { MaxDrift = value };

        public static SimConfig WithWithdrawalRate(this SimConfig config, double value) 
            => config with { PCTWithdrawal = config.PCTWithdrawal with { FirstYearPct = value } };

        public static SimConfig WithWithdrawalIncrement(this SimConfig config, double value) 
            => config with { PCTWithdrawal = config.PCTWithdrawal with { IncrementPct = value } };

        public static SimConfig WithBufferAmount(this SimConfig config, double value) 
            => config with { UseBufferCash = config.UseBufferCash with { Amount = value } };

        public static SimConfig WithAnnualFees(this SimConfig config, double value) 
            => config with { Fees = config.Fees with { AnnualFeesPct = value } };
    }
}
