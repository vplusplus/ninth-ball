
namespace NinthBall
{
    /// <summary>
    /// The root data structure representing a complete simulation configuration.
    /// This is the "Domain Contract" that can be deserialized from YAML or populated by a UI.
    /// </summary>
    public sealed record SimConfig
    (
        string? RandomSeedHint,
        string? Output,
        SimParams SimParams,
        InitialBalance InitialBalance,

        // Historical Data & Bootstrapping
        ROIHistory? ROIHistory,
        Growth? Growth,
        FlatBootstrap? FlatBootstrap,
        MovingBlockBootstrap? MovingBlockBootstrap,
        ParametricBootstrap? ParametricBootstrap,

        // Strategies (Optional)
        Rebalance? Rebalance,
        Reallocate? Reallocate,
        FeesPCT? FeesPCT,
        Taxes? Taxes,
        AdditionalIncomes? AdditionalIncomes,
        LivingExpenses? LivingExpenses,
        PrecalculatedLivingExpenses? PrecalculatedLivingExpenses,
        FixedWithdrawal? FixedWithdrawal,
        PercentageWithdrawal? PercentageWithdrawal,
        VariablePercentageWithdrawal? VariablePercentageWithdrawal,
        UseBufferCash? UseBufferCash,
        BufferRefill? BufferRefill
    );
}
