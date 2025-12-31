
namespace NinthBall
{
    /// <summary>
    /// Percentiles generted on html and excel outputs.
    /// </summary>
    internal sealed class Percentiles
    {
        public readonly record struct PCT(double Pctl, string Caption, string Tag);

        public static readonly IReadOnlyList<PCT> Items = 
        [
            new(0.00, "0th",  "Disaster"),
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
