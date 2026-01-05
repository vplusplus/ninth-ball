# Input Configuration Reference

This document describes all configuration parameters available in the YAML input file.

## Simulation Parameters

### SimParams
Core simulation settings.

- **StartAge** - Starting age for the simulation (50 to 100)
- **NoOfYears** - Number of years to simulate (1 to 100)
- **Iterations** - Number of Monte Carlo iterations to run (1 to 50,000, default: 10,000)

### RandomSeedHint
Optional text hint for random number generator seed. Using the same hint ensures reproducible results across runs.

## Portfolio Configuration

### InitialBalance
Define starting balances and allocations for three account types.

Each account type has:
- **Amount** - Initial balance in dollars
- **Allocation** - Percentage allocated to stocks (0.0 to 1.0, e.g., 0.6 = 60%)

Account types:
- **PreTax** - Tax-deferred accounts (e.g., 401K, Traditional IRA)
- **PostTax** - Taxable investment accounts
- **Cash** - Cash/savings accounts (Allocation is ignored for Cash)

Example:
```yaml
InitialBalance:
  PreTax:
    Amount: 1,000,000
    Allocation: 60%
  PostTax:
    Amount: 1,000,000
    Allocation: 60%
  Cash:
    Amount: 0
    Allocation: 0%
```

## Portfolio Management

### Rebalance
Rebalance portfolio when allocation drifts from target.

- **MaxDrift** - Maximum allowed drift from target allocation before rebalancing (0.0 to 0.5, e.g., 0.05 = 5%)

### Reallocate
Age-based reallocation (glide path) to gradually shift allocation over time.

- **MaxDrift** - Maximum allowed drift before reallocation (0.0 to 0.5)
- **Steps** - List of age-based allocation changes
  - **AtAge** - Age at which to change allocation (1 to 100)
  - **Allocation** - Target stock allocation at this age (0.0 to 1.0)

Example:
```yaml
Reallocate:
  MaxDrift: 5%
  Steps:
    - AtAge: 60
      Allocation: 60%
    - AtAge: 70
      Allocation: 50%
    - AtAge: 80
      Allocation: 40%
```

## Income and Expenses

### AdditionalIncomes
Additional income sources beyond portfolio withdrawals.

- **SS** - Social Security income
  - **FromAge** - Age when income starts (1 to 100)
  - **Amount** - Annual income amount in dollars
  - **Increment** - Annual increase percentage (0.0 to 1.0, e.g., 0.02 = 2%)
- **Ann** - Annuity income (same structure as SS)

### LivingExpenses
Annual living expenses with inflation adjustments and age-based step-downs.

- **FirstYearAmount** - Initial annual expenses in dollars
- **Increment** - Annual increase percentage (0.0 to 1.0, e.g., 0.02 = 2% for inflation)
- **StepDown** - List of age-based expense reductions
  - **AtAge** - Age at which to reduce expenses (50+)
  - **Reduction** - Dollar amount to reduce expenses by

Example:
```yaml
LivingExpenses:
  FirstYearAmount: 100,000
  Increment: 2%
  StepDown:
    - AtAge: 65
      Reduction: 15000    # Example: Stop paying life insurance
    - AtAge: 70
      Reduction: 10000    # Example: Medicare reduces medical insurance costs
```

### PrecalculatedLivingExpenses
Load custom expense schedule from an Excel file (alternative to LivingExpenses).

- **FileName** - Path to Excel file (supports `$(MyPath)` placeholder)
- **SheetName** - Worksheet name containing expense data (first column contains amounts)

## Withdrawal Strategies

Choose **one** of the following withdrawal strategies for PreTax accounts.

### FixedWithdrawal
Fixed dollar amount with annual increments.

- **FirstYearAmount** - Initial withdrawal amount in dollars
- **Increment** - Annual increase percentage (0.0 to 1.0)

### PercentageWithdrawal
Percentage-based withdrawal with optional increments and age-based resets.

- **FirstYearPct** - Initial withdrawal percentage (0.0 to 1.0, e.g., 0.04 = 4%)
- **Increment** - Annual increment to withdrawal percentage (0.0 to 1.0)
- **ResetAtAge** - List of ages at which to reset withdrawal back to FirstYearPct

Example:
```yaml
PercentageWithdrawal:
  FirstYearPct: 4%
  Increment: 2%
  ResetAtAge: [65, 70]
```

### VariablePercentageWithdrawal
Dynamic withdrawal based on portfolio performance.

- **ROI** - Expected return on investment (0.0 to 1.0)
- **Inflation** - Expected inflation rate (0.0 to 1.0)
- **Floor** - Minimum withdrawal amount (optional, dollars)
- **Ceiling** - Maximum withdrawal amount (optional, dollars)

### RMD
Required Minimum Distributions from PreTax accounts.

- **StartAge** - Age to begin RMDs (70 to 80, default: 73)

## Fees and Taxes

### FeesPCT
Annual management fees for each account type.

- **PreTax** - Annual fees as percentage of PreTax account (0.0 to 1.0)
- **PostTax** - Annual fees as percentage of PostTax account (0.0 to 1.0)
- **Cash** - Annual fees as percentage of Cash account (0.0 to 1.0)

Example:
```yaml
FeesPCT:
  PreTax:   0.2%    # 401K account
  PostTax:  0.9%    # Joint investments
  Cash:     0%      # High-yield savings
```

### Taxes
Tax modeling for withdrawals and income.

- **YearZeroTaxAmount** - Initial tax liability in dollars (for prior year taxes)
- **TaxRates**
  - **OrdinaryIncome** - Tax rate for ordinary income (0.0 to 1.0)
  - **CapitalGains** - Tax rate for capital gains (0.0 to 1.0)

## Growth Strategies

### Growth
Configure market return modeling.

- **Bootstrapper** - Bootstrap method to use (Flat | Sequential | MovingBlock | Parametric)
- **CashROI** - Annual return on cash accounts (0.0 to 1.0)

### ROIHistory
Historical market data for Sequential and MovingBlock bootstrapping.

- **XLFileName** - Path to Excel file containing historical returns (supports `$(MyPath)` placeholder)
- **XLSheetName** - Worksheet name (columns: Year, StocksROI, BondROI)

### FlatBootstrap
Constant annual returns (useful for baseline testing).

- **Stocks** - Annual stock return (0.0 to 1.0)
- **Bonds** - Annual bond return (0.0 to 1.0)

### MovingBlockBootstrap
Random blocks of historical data preserving temporal patterns.

- **BlockSizes** - List of block sizes for bootstrap sampling (e.g., [3, 4, 5] means 3, 4, or 5-year blocks)
- **NoConsecutiveBlocks** - If true, prevent consecutive blocks with overlapping years (stress-testing feature)

### ParametricBootstrap
Statistical distribution-based returns with configurable parameters.

- **DistributionType** - Distribution type (currently supports "LogNormal")
- **StocksBondCorrelation** - Correlation between stocks and bonds (-1.0 to 1.0)
- **Stocks** - Stock distribution parameters
  - **MeanReturn** - Mean annual return (-1.0 to 1.0)
  - **Volatility** - Standard deviation of returns (0.0 to 1.0)
  - **Skewness** - Distribution skewness (-10.0 to 10.0, negative = downside bias)
  - **Kurtosis** - Distribution kurtosis (0.0 to 100.0, higher = fatter tails, normal = 3.0)
  - **AutoCorrelation** - Year-to-year correlation (-1.0 to 1.0, models market regimes)
- **Bonds** - Bond distribution parameters (same structure as Stocks)

Example:
```yaml
ParametricBootstrap:
  DistributionType: LogNormal
  StocksBondCorrelation: -0.15
  Stocks:
    MeanReturn: 6.5%
    Volatility: 17%
    Skewness: -0.3
    Kurtosis: 4.0
    AutoCorrelation: 0.05
  Bonds:
    MeanReturn: 3.5%
    Volatility: 8%
    Skewness: 0
    Kurtosis: 3
    AutoCorrelation: 0
```

## Special Features

### Disabling Configurations
Prefix any configuration section name with `ZZZ` to disable it (e.g., `ZZZPercentageWithdrawal` is ignored).

### Path Placeholder
Use `$(MyPath)` in file paths to reference the directory containing the YAML configuration file. This is replaced at runtime with the absolute path.

Example:
```yaml
ROIHistory:
  XLFileName: $(MyPath)/ROI-History.xlsx
  XLSheetName: DATA
```

## Example Configuration

See [Input.yaml](src/NinthBall/Input/Input.yaml) for a complete working example.
