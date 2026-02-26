# NinthBall

**Beyond the 8-Ball** – A Monte Carlo simulation framework for financial planning, grounded in math, probability, and historical economic cycles.

## What It Does

NinthBall runs thousands of simulated scenarios to help you understand how your portfolio might perform under different market conditions. Instead of relying on simple assumptions, it uses a **Regime Aware** model that simulates shifts between different economic states—like Bull markets, Crises, and Inflationary periods—based on over a century of historical data.

## Key Features

### Multi-Account Portfolio Support
- **PreTax accounts** (401K, Traditional IRA) with RMD calculations
- **PostTax accounts** (taxable investments) with capital gains tax modeling
- **Cash accounts** (savings, money market) with configurable returns
- Independent allocation and fee structures for each account type

### Tax-Aware Planning
- **Federal and State taxation**: Ordinary income and capital gains tax modeling
- **State-specific rules**: Deep modeling for specific states (e.g., New Jersey) including pension exclusions and income-based cliffs
- **Efficient sequencing**: Tax-aware withdrawal patterns across different account types
- **RMDs**: Automatic Required Minimum Distributions starting at age 73 per IRS rules

### Income and Expense Modeling
- **Reliable income**: Social Security and annuities with age-based triggers and automatic inflation adjustments
- **Living expenses**: Annual spending targets with inflation and age-based step-downs (e.g., life insurance or Medicare transitions)

### Advanced "Regime Aware" Simulation
- **Historical replay**: Sequential or random blocks of history that respect historical sequencing
- **Economic cycles**: Simulation engine understands the "gravity" of economic regimes, effectively modeling transitions between Bull, Crisis, and Inflationary states
- **Stress-testing**: Parametric modeling with "fat tails" to capture rare but extreme market events that standard models often miss

### Sophisticated Withdrawal Strategies
- **Fixed & Percentage**: Consistent withdrawal patterns with annual increments
- **Variable Adjustment**: Tax-optimized amortization toward a zero-balance, featuring inflation-adjusted **Floor** and **Ceiling** guardrails
- **Guardrails**: Ensure you never withdraw too little for basic needs or too much to trigger a massive tax bill

### Portfolio Management
- **Smart Rebalancing**: Maintain your target allocation with configurable drift tolerance
- **Glide Paths**: Automated age-based reallocation to reduce risk as you get older
- Account-specific annual fees and transparent expense modeling

### Comprehensive Analytics
- **Outcome Distributions**: View results across multiple percentiles (10th, 25th, 50th, 90th) to understand the "safe" vs. "risky" paths
- **Survival Metrics**: Clear probability of portfolio survival across all scenarios
- **Failure Analysis**: "Failure buckets" to identify exactly *when* a strategy might fall short in a long-term plan
- **Detailed Reports**: Professional HTML and Excel reports with year-by-year breakdowns

## How It Works

1. **Configure** your scenario using a simple, readable YAML file:
   - Starting age, time horizon, and account balances
   - Withdrawal strategy and tax settings
   - Living expenses and income triggers
   - Choice of simulation method (Historical, Random, or Parametric)

2. **Run** the simulation:
   ```
   NinthBall --in Input.yaml
   ```
   
   Or use **watch mode** to see results update instantly as you tweak your plan:
   ```
   NinthBall --in Input.yaml --watch
   ```

3. **Review** the generated reports to see your probability of success and identify where your strategy can be optimized.

## What Makes It Different

- **Sequencing Matters**: Unlike simple Monte Carlo tools that treat every year as an independent coin flip, NinthBall respects the **Regime Aware** nature of markets, recognizing that a "Crisis" year behaves differently than a "Bull" year.
- **Tax Accuracy**: It doesn't just "apply a tax rate"; it models the actual flow of funds through federal and state tax rules.
- **Fast and Local**: Runs thousands of iterations in seconds on your own machine. No data leaves your computer.

---

*NinthBall helps you move beyond simple rules of thumb to make data-informed decisions about your financial future.*
