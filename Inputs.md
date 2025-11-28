# Input Configuration Reference

This document describes all configuration parameters available in the YAML input file.

## General Configuration

- **RandomSeedHint** - Text hint for random number generator seed (ensures reproducible results across runs)
- **StartingBalance** - Initial portfolio balance in dollars
- **StocksAllocationPct** - Initial allocation to stocks as a percentage (0.0 to 1.0, e.g., 0.6 = 60%)
- **MaxDrift** - Maximum allowed drift from target allocation before rebalancing (e.g., 0.03 = 3%)
- **NoOfYears** - Number of years to simulate (1 to 50)
- **Iterations** - Number of Monte Carlo iterations to run (minimum 1, typically 10,000+)
- **Output** - Path to the output HTML report file

## Withdrawal Strategies

Choose **exactly one** withdrawal strategy.

### PCTWithdrawal
Percentage-based withdrawal with optional increments and resets.

- **FirstYearPct** - Withdrawal percentage in the first year (0.0 to 1.0, e.g., 0.04 = 4%)
- **IncrementPct** - Annual increment to withdrawal percentage (0.0 to 1.0, e.g., 0.02 = 2% increase per year)
- **ResetYears** - Optional list of years to reset withdrawal back to FirstYearPct (e.g., [6, 11])

### PrecalculatedWithdrawal
Custom withdrawal amounts loaded from an Excel file.

- **FileName** - Path to Excel file containing withdrawal schedule (supports `$(MyPath)` placeholder)
- **SheetName** - Name of the worksheet containing withdrawal data (first column contains amounts)

## Withdrawal Optimization Strategies

Both strategies are optional and can be used together.

### UseBufferCash
Tap into reserve cash during poor market years.

- **Amount** - Buffer cash amount in dollars (must be > 0)
- **GrowthThreshold** - Portfolio growth threshold below which buffer is used (-0.1 to 1.0, e.g., 0.03 = 3%)

### ReduceWithdrawal
Temporarily reduce withdrawals when portfolio underperforms.

- **MaxSkips** - Maximum number of times withdrawal can be reduced (0 to 100)
- **CutOffYear** - Only apply reduction in first N years (1 to 100)
- **GrowthThreshold** - Portfolio growth threshold below which withdrawal is reduced (-0.1 to 1.0)
- **ReductionPct** - Percentage to reduce withdrawal by (0.0 to 1.0, e.g., 0.4 = 40% reduction, 1.0 = skip entirely)

## Growth Strategies

Choose **exactly one** growth strategy.

### FlatGrowth
Assume constant annual returns.

- **StocksGrowthRate** - Annual stock growth rate (0.0 to 1.0, e.g., 0.07 = 7%)
- **BondGrowthRate** - Annual bond growth rate (0.0 to 1.0, e.g., 0.03 = 3%)

### HistoricalGrowth
Use historical market data with Moving Block Bootstrap methodology.

- **FileName** - Path to Excel file containing historical returns (supports `$(MyPath)` placeholder)
- **SheetName** - Name of the worksheet containing historical data (columns: Year, StocksROI, BondROI)
- **UseRandomBlocks** - If true, use random block sampling; if false, use sequential historical data
- **BlockSizes** - List of block sizes for bootstrap sampling (e.g., [3, 4, 5] means 3, 4, or 5-year blocks)
- **NoConsecutiveBlocks** - If true, prevent consecutive blocks with overlapping years (stress-testing feature)
- **Skip1931** - If true, exclude 1931 (worst historical year) from sampling (exploratory feature)

## Fees

Optional fee configuration.

### Fees
- **AnnualFeesPct** - Annual fees as a percentage of portfolio (0.0 to 0.02, e.g., 0.009 = 0.9%)

## Special Features

### Disabling Configurations
Prefix any configuration section name with `zz` or `zzz` to disable it (e.g., `zzPCTWithdrawal` is ignored).

### Path Placeholder
Use `$(MyPath)` in file paths to reference the directory containing the YAML configuration file. This is replaced at runtime with the absolute path.

Example:
```yaml
FileName: $(MyPath)/DATA/ROI-History.xlsx
```

## Example Configuration

See [Input.yaml](src/NinthBall/Input/Input.yaml) for a complete working example.
