
## What-if: Different growth objectives 


### Results

| GrowthStrategy         | NumIterations | SurvivalRate | Balance(r) 5th | Balance(r) 10th | Balance(r) 15th | Balance(r) 20th |
|:-----------------------|--------------:|-------------:|---------------:|----------------:|----------------:|----------------:|
| FlatGrowth             |             1 |         100% |     $2,622,130 |      $2,622,130 |      $2,622,130 |      $2,622,130 |
| HistoricalGrowth       |            66 |         100% |     $5,124,955 |      $5,393,356 |      $5,573,729 |      $6,245,086 |
| RandomHistoricalGrowth |        10,000 |          99% |     $1,210,602 |      $2,142,156 |      $2,958,749 |      $3,876,361 |
| RandomGrowth           |        10,000 |          96% |       $264,625 |        $867,816 |      $1,528,787 |      $2,294,087 |


### Input(s):

``` json
{
  "StartAge": 63,
  "NoOfYears": 32,
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
{
  "YearZeroCashBalance": 12000,
  "YearZeroTaxAmount": 25000,
  "PreTax": {
    "Amount": 2000000,
    "Allocation": 0.7
  },
  "PostTax": {
    "Amount": 2000000,
    "Allocation": 0.7
  }
}
```


