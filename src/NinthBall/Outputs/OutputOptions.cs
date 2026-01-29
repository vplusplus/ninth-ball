
namespace NinthBall.Outputs
{
    /// <summary>
    /// Immutable structure that describes output configurations.
    /// </summary>
    public sealed record OutputOptions
    (
        IReadOnlyList<double>   Percentiles,
        IReadOnlyList<int>      Iterations,
        IReadOnlyList<CID>      HtmlColumns,
        IReadOnlyList<CID>      ExcelColumns
    );
}
