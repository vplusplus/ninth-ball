
# Monte Carlo Financial planning what-if Simulation Dataset

This dataset represents summary view of multiple what-if questions against a Monte Carlo financial planning simulations.
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

# Survival matrix prsentation guidelines

Present Survival rate as a matrix with following format:
* Present survival rate as a matrix
* Present one matrix for each initial balance
* Use First year expense for the rows and start age for the columns
* Display survival rates as whole percentages
* Suppress survival rate (display blank) if the survival rate is below suggested target survival threshold
* Do not add citation links to individual cells to improve readability.
