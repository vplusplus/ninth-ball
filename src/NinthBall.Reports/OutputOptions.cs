
using NinthBall.Utils;
using System.ComponentModel.DataAnnotations;

namespace NinthBall.Reports
{
    public sealed record OutputOptions
    (
        [property: Required, ValidateNested] HtmlOutputOptions  Html,
        [property: Required, ValidateNested] ExcelOutputOptions Excel
    );

    public readonly record struct HtmlOutputOptions
    (
        [property: Required] string File,
        [property: Required] string View,
        IReadOnlyList<double>? Percentiles,
        IReadOnlyList<int>?    Iterations
    );

    public readonly record struct ExcelOutputOptions
    (
        [property: Required] string File,
        [property: Required] string View,
        IReadOnlyList<double>? Percentiles,
        IReadOnlyList<int>? Iterations
    );

    public sealed record OutputDefaults
    (
        [property: Required] IReadOnlyList<double> Percentiles,
        [property: Required] IReadOnlyDictionary<string, IReadOnlyList<CID>> Views
    );

}
