
namespace NinthBall
{
    /// <summary>
    /// Percentiles presented in the html and excel outputs.
    /// </summary>
    internal sealed class Percentiles
    {
        public readonly record struct PCTL(double Pctl, string PctlName, string FriendlyName);

        public static readonly IReadOnlyList<PCTL> Items = 
        [
            new(0.00, "0th",  "Worst-case"),
            new(0.05, "5th",  "Unlucky"),
            new(0.10, "10th", "Unfortunate"),
            new(0.20, "20th", "Target"),
            new(0.50, "50th", "Coin-flip"),
            new(0.80, "80th", "Fortunate"),
            new(0.90, "90th", "Lucky"),
        ];

        public const double Target = 0.20;
    }
}
