
## What-if: Different growth objectives 


### Input

| From         | To           | PreTax       | PostTax      | Year1        |
|:-------------|:-------------|:-------------|:-------------|:-------------|
| 60 years     | 95 years     | $1.5 M       | $1.5 M       | $130 K       |

### Results

| GrowthStrategy         | NumIterations | SurvivalRate | Balance(r) 5th | Balance(r) 10th | Balance(r) 15th | Balance(r) 20th |
|:-----------------------|--------------:|-------------:|---------------:|----------------:|----------------:|----------------:|
| FlatGrowth             |             1 |         100% |     $1,618,017 |      $1,618,017 |      $1,618,017 |      $1,618,017 |
| HistoricalGrowth       |            63 |         100% |     $2,640,936 |      $2,923,376 |      $3,562,967 |      $3,776,280 |
| RandomHistoricalGrowth |        10,000 |          97% |       $408,114 |      $1,261,125 |      $1,991,320 |      $2,694,944 |
| RandomGrowth           |        10,000 |          93% |        $32,362 |        $320,627 |        $825,534 |      $1,423,664 |


### Input details:


#### Simulation params:

``` json
{
  "StartAge": 60,
  "NoOfYears": 35,
  "Iterations": 10000,
  "Objectives": [
    "YearlyRebalance",
    "AdditionalIncomes",
    "LivingExpenses",
    "AnnualFees",
    "TieredTax",
    "VariableWithdrawal",
    "RequiredMinimumDistribution"
  ]
}
```


#### Inittial:

``` json
{
  "YearZeroCashBalance": 12000,
  "YearZeroTaxAmount": 25000,
  "PreTax": {
    "Amount": 1500000,
    "Allocation": 0.7
  },
  "PostTax": {
    "Amount": 1500000,
    "Allocation": 0.7
  }
}
```


#### Expenses:

``` json
{
  "FirstYearAmount": 130000,
  "StepDown": [
    {
      "AtAge": 65,
      "Reduction": 28000
    },
    {
      "AtAge": 70,
      "Reduction": 6000
    }
  ]
}
```


