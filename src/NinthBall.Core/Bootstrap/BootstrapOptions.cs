
using System.ComponentModel.DataAnnotations;

namespace NinthBall.Core
{
    // FlatBootstrapper configurations
    public sealed record FlatGrowth
    (
        [property: Range(0.0001, 1)] double Stocks,
        [property: Range(0.0001, 1)] double Bonds,
        [property: Range(0.0001, 1)] double InflationRate
    );


    // Block centric bootstrapper configurations
    public sealed record BootstrapOptions
    (
        [property: Required] 
        IReadOnlyList<int> BlockSizes,

        [property: Range(0.0, 1.0)] 
        double RegimeAwareness
    );

    // One or more named parametric profile configurations
    public sealed record ParametricProfiles
    (
        [property: Required] string Current,
        [property: Required] IReadOnlyDictionary<string, Regime> Profiles
    );

}
