

namespace UnitTests.WhatIf
{
    public readonly record struct MinMaxSteps
    (
        double Min,
        double Max,
        double Steps,
        double Target
    );

    public sealed record WhatIfOptions
    (
        double TargetSurvivalRate, 
        MinMaxSteps InitialBalance, 
        MinMaxSteps FirstYearExpense, 
        MinMaxSteps StartAge
    );
}
