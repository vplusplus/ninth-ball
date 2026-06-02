
using NinthBall.Utils;
using System.ComponentModel.DataAnnotations;

namespace NinthBall.Reports
{
    public sealed record OutputOptions
    (
        [property: ValidateNested] HtmlOutputOptions  Html,
        [property: ValidateNested] ExcelOutputOptions Excel
    );

    public readonly record struct HtmlOutputOptions
    (
        string File,
        string View,
        IReadOnlyList<double>?  Percentiles,
        IReadOnlyList<int>?     Iterations
    );

    public readonly record struct ExcelOutputOptions
    (
        string File,
        string View,
        IReadOnlyList<double>?  Percentiles,
        IReadOnlyList<int>?     Iterations
    );

    public sealed record OutputDefaults
    (
        [property: Required]    IReadOnlyList<double> Percentiles,
        [property: Required]    IReadOnlyDictionary<string, IReadOnlyList<CID>> Views,
        [property: Range(0,1)]  double TargetPercentile
    );

}
