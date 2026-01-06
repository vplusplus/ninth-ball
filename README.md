# NinthBall

**Beyond the 8-Ball** – A Monte Carlo simulation framework for financial planning, grounded in math and probability.

## What It Does

NinthBall runs thousands of simulated scenarios to help you understand how your portfolio might perform under different market conditions. Instead of relying on simple assumptions, it uses historical market data and statistical methods to model realistic outcomes across multiple account types with tax-aware strategies for long-term financial planning.

## Key Features

### Multi-Account Portfolio Support
- **PreTax accounts** (401K, Traditional IRA) with RMD calculations
- **PostTax accounts** (taxable investments) with capital gains tax modeling
- **Cash accounts** (savings, money market) with configurable returns
- Independent allocation and fee structures for each account type

### Tax-Aware Planning
- Ordinary income and capital gains tax rates
- Year-zero tax liability for prior year taxes
- Tax-efficient withdrawal sequencing across account types
- Required Minimum Distributions (RMD) starting at age 73 (IRS rules)

### Income and Expense Modeling
- **Additional income sources**: Social Security and annuities with age triggers and inflation adjustments
- **Living expenses**: Annual expenses with inflation and age-based step-downs (e.g., life insurance, Medicare transitions)

### Advanced Bootstrap Methods
- **Flat**: Constant returns for baseline testing
- **Sequential**: Historical data in chronological order
- **Moving Block**: Random blocks of historical data preserving temporal patterns and market correlations
- **Parametric**: Statistical distribution-based (LogNormal) with configurable skewness, kurtosis, and autocorrelation for stress-testing

### Flexible distribution of tax-deferred assets
- **Fixed**: Dollar amount with annual increments
- **Percentage**: Percentage-based with increments and age-based resets
- **Variable Percentage**: Dynamic withdrawals with tax-optimized amortization towards zero balance, featuring floor/ceiling constraints
- **RMD**: Required Minimum Distributions from PreTax accounts

### Portfolio Management
- **Rebalancing**: Maintain target allocation with drift tolerance
- **Reallocation (Glide Path)**: Age-based allocation changes for risk reduction over time
- Account-specific annual fees

### Comprehensive Analysis
- Run 10,000+ iterations to capture full range of outcomes
- View results across multiple percentiles to understand the distribution of outcomes
- **HTML reports** with detailed statistics, percentile analysis, and year-by-year breakdowns
- **Excel reports** with same data in spreadsheet format for custom analysis
- Both nominal and inflation-adjusted balances presented

## How It Works

1. **Configure** your simulation using a simple YAML file:
   - Starting age and time horizon
   - Initial balances and allocations for PreTax, PostTax, and Cash accounts
   - Withdrawal strategy and RMD settings
   - Tax rates and additional income sources
   - Living expenses with step-downs
   - Bootstrap method and number of iterations

2. **Run** the simulation:
   ```
   NinthBall --in Input.yaml
   ```
   
   Specify a custom output path:
   ```
   NinthBall --in Input.yaml --out MyResults.html
   ```
   
   Or use **watch mode** for continuous regeneration:
   ```
   NinthBall --in Input.yaml --watch
   ```

   To generate a **sample input** file:
   ```
   NinthBall --sampleinput
   ```

3. **Review** the generated reports showing:
   - Probability of portfolio survival
   - Expected ending balances at different percentiles (10th, 25th, 50th, 90th)
   - Year-by-year performance across scenarios
   - Tax liabilities and withdrawal patterns
   - Account-specific balances and allocations

## What Makes It Different

- **Mathematically Sound**: Multiple bootstrap methods including parametric distributions with fat tails and autocorrelation
- **Configurable**: All parameters defined in readable YAML files
- **Fast**: Runs thousands of iterations in seconds
- **Transparent**: Open methodology based on established statistical principles

## Use Cases

- Stress-testing portfolio strategies against historical market conditions
- Tax-efficient withdrawal strategy optimization across account types
- Comparing different asset allocations, withdrawal approaches, and reallocation strategies
- Understanding the impact of fees, Social Security timing, and expense changes

---

*NinthBall helps you move beyond simple rules of thumb to make data-informed decisions about your financial future.*
