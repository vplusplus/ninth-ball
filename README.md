# NinthBall

**Beyond the 8-Ball** – A Monte Carlo simulation framework for portfolio planning, grounded in math and probability.

## What It Does

NinthBall runs thousands of simulated scenarios to help you understand how your portfolio might perform under different market conditions. Instead of relying on simple assumptions, it uses historical market data and statistical methods to model realistic outcomes.

## Key Features

### Historical Data-Driven Simulations
- Uses actual historical stock and bond returns
- Employs Moving Block Bootstrap methodology to preserve realistic market patterns
- Accounts for market volatility, sequences of returns, and temporal dependencies

### Flexible Withdrawal Strategies
- Percentage-based withdrawals with optional annual increments and periodic resets
- Pre-calculated custom withdrawal schedules from Excel
- Adaptive strategies that respond to market performance

### Optimization Techniques
- **Buffer Cash**: Tap into reserve funds during poor market years
- **Dynamic Withdrawal Reduction**: Temporarily reduce withdrawals when portfolio underperforms
- Configurable rebalancing with drift tolerance

### Comprehensive Analysis
- Run 10,000+ iterations to capture full range of outcomes
- View results across multiple percentiles to understand the distribution of outcomes
- HTML reports with detailed statistics and visualizations
- Both nominal and inflation-adjusted (real) balances presented

## How It Works

1. **Configure** your simulation using a simple YAML file:
   - Starting balance and asset allocation
   - Withdrawal strategy
   - Time horizon (years)
   - Number of iterations
   
   See [Inputs.md](Inputs.md) for complete configuration reference.

2. **Run** the simulation:
   ```
   NinthBall --in Input.yaml
   ```

3. **Review** the generated HTML report showing:
   - Probability of portfolio survival
   - Expected ending balances at different percentiles
   - Year-by-year performance across scenarios

## What Makes It Different

- **Mathematically Sound**: Uses Moving Block Bootstrap to maintain realistic correlation between consecutive years
- **Configurable**: All parameters defined in readable YAML files
- **Fast**: Runs thousands of iterations in seconds
- **Transparent**: Open methodology based on established statistical principles
- **Practical**: Supports real-world scenarios like variable withdrawals and emergency reserves

## Use Cases

- Portfolio planning and "safe withdrawal rate" analysis
- Stress-testing portfolio strategies against historical market conditions
- Comparing different asset allocations and withdrawal approaches
- Understanding the impact of fees, inflation adjustments, and market timing

---

*NinthBall helps you move beyond simple rules of thumb to make data-informed decisions about your financial future.*
