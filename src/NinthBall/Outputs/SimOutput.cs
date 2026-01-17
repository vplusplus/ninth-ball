
namespace NinthBall.Outputs
{
    /// <summary>
    /// Immutable structure that describes output configurations.
    /// </summary>
    public readonly record struct SimOutput
    (
        IReadOnlyList<double> Percentiles,
        IReadOnlyList<CID>  HtmlColumns,
        IReadOnlyList<CID>  ExcelColumns
    );
}
