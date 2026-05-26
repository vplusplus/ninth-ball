
# Monte Carlo Financial planning what-if Simulation Dataset

This dataset contains Monte Carlo financial planning simulations used to compare multiple what-if strategies.
All values are synthetic and contain **no PII**.

## About the dataset

### 1. `Assumptions` 

A list of modeling assumptions applied uniformly across all simulations.  

### 2. `WhatIfOptions`

Defines the parameter ranges used to run multiple independent Monte Carlo simulations.

Notes:

`FirstYearExpense` represents first-year lifestyle spending only. This is not necessarily equal to portfolio withdrawal because:
	- Taxes may require additional withdrawals
	- Passive income may offset spending
	- Pretax drawdown strategies may accelerate withdrawals
	- Future spending adjusts dynamically with simulated inflation.

`Target` represents the currently selected baseline scenario.

### 3. `WhatIfResults`

An array where **each entry represents one scenario** produced from an independent simulation.

---

# Presentation Guidance

* Matrix layouts are preferred for presenting survival rate.
* Display survival rates as whole percentages
* Optionally suppress survival rate below target survival threshold (display blank)

