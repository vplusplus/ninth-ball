
namespace NinthBall
{
    // Partial classes for 

    public partial record SimConfig
    {
        public SimConfig ThrowIfInvalid()
        {
            StocksAllocationPct.ThrowIfBadPCT("StocksAllocationPct", min: 0.0);

            if (NoOfYears < 1 || NoOfYears > 50) throw new FatalWarning("NoOfYears must be between 1 to 50");
            if (Iterations < 1) throw new FatalWarning("Iterations must be >= 1");
            if (string.IsNullOrWhiteSpace(Output)) throw new FatalWarning("Specify Output");

            if (null != PCTWithdrawal && null != PrecalculatedWithdrawal) throw new FatalWarning("Withdrawals: Specify ONLY ONE withdrawal strategy");
            if (null == PCTWithdrawal && null == PrecalculatedWithdrawal) throw new FatalWarning("Withdrawals: Specify at least ONE withdrawal strategy");
            if (null != FlatGrowth && null != HistoricalGrowth) throw new FatalWarning("Growth: Specify ONLY ONE Growth strategy");
            if (null == FlatGrowth && null == HistoricalGrowth) throw new FatalWarning("Growth: Specify at least ONE Growth strategy");

            PCTWithdrawal?.ThrowIfInvalid();
            PrecalculatedWithdrawal?.ThrowIfInvalid();

            UseBufferCash?.ThrowIfInvalid();
            ReduceWithdrawal?.ThrowIfInvalid();

            FlatGrowth?.ThrowIfInvalid();
            HistoricalGrowth?.ThrowIfInvalid();

            Fees?.ThrowIfInvalid();
            MaxDrift.ThrowIfBadPCT("MaxDrift");

            return this;
        }
    }

    public partial record PCTWithdrawal
    {
        public void ThrowIfInvalid()
        {
            FirstYearPct.ThrowIfBadPCT("PCTWithdrawal.FirstYearPct", min: 0.0);
            IncrementPct.ThrowIfBadPCT("PCTWithdrawal.IncrementPct", min: 0.0);
        }
    }

    public partial record PrecalculatedWithdrawal
    {
        public void ThrowIfInvalid()
        {
            FileName.ThrowIfMissingFile("Precalculated withdrawal file");
        }
    }

    public partial record UseBufferCash
    {
        public void ThrowIfInvalid()
        {
            if (Amount <= 0.0) throw new FatalWarning("UseBufferCash: Specify Buffer cash amount.");
            GrowthThreshold.ThrowIfBadPCT("UseBufferCash.GrowthThreshold");
        }
    }

    public partial record ReduceWithdrawal
    {
        public void ThrowIfInvalid()
        {
            if (MaxSkips < 0 || MaxSkips > 100) throw new FatalWarning("ReduceWithdrawal.MaxSkips must be >= 0 and <= 100");
            if (CutOffYear < 1 || CutOffYear > 100) throw new FatalWarning("ReduceWithdrawal.CutOffYear must be >= 1 and <= 100");
            GrowthThreshold.ThrowIfBadPCT("ReduceWithdrawal.GrowthThreshold");
            ReductionPct.ThrowIfBadPCT("ReduceWithdrawal.ReductionPct");
        }
    }

    public partial record FlatGrowth
    {
        public void ThrowIfInvalid()
        {
            StocksGrowthRate.ThrowIfBadPCT("FlatGrowth.StocksGrowthRate", min: 0.0);
            BondGrowthRate.ThrowIfBadPCT("FlatGrowth.BondGrowthRate", min: 0.0);
        }
    }

    public partial record HistoricalGrowth
    {
        public void ThrowIfInvalid()
        {
            FileName.ThrowIfMissingFile("Historical growth file");
            if (string.IsNullOrWhiteSpace(SheetName)) throw new FatalWarning("HistoricalGrowth: Specify SheetName.");
            if (UseRandomBlocks && (null == BlockSizes || BlockSizes.Count == 0)) throw new FatalWarning("HistoricalGrowth: Specify BlockSizes.");
        }
    }

    public partial record Fees
    {
        public void ThrowIfInvalid()
        {
            AnnualFeesPct.ThrowIfBadPCT("Fees.AnnualFeesPct", min: 0.0, max: 0.02);
        }
    }



    public static class SimConfigValidations
    {
        public static void ThrowIfBadPCT(this double value, string name, double min = -0.1, double max = 1.0)
        {
            if (value < min || value > max) throw new FatalWarning($"{name}: Invalid value {value:#.0} | Must be between {min:#.0} and {max:#.0} | Example: 3% is 0.03");
        }

        public static void ThrowIfMissingFile(this string fileName, string purpose)
        {
            if (string.IsNullOrWhiteSpace(fileName)) throw new FatalWarning($"{purpose}: Specify file name.");
            if (!File.Exists(fileName)) throw new FatalWarning($"{purpose}: File not found | {fileName}");
        }
    }
}

