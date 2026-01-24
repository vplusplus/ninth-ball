
namespace NinthBall.Outputs
{
    /// <summary>
    /// Immutable structure that describes output configurations.
    /// </summary>
    public readonly record struct SimOutput
    (
        IReadOnlyList<double>   Percentiles,
        IReadOnlyList<int>      Iterations,
        IReadOnlyList<CID>      HtmlColumns,
        IReadOnlyList<CID>      ExcelColumns
    );
}
