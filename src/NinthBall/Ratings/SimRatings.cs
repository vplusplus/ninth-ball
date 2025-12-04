
namespace NinthBall
{
    /// <summary>
    /// Represents configuration and corresponding implementation of Simuation results rating scheme.
    /// </summary>
    public record SimRatings
    (
        SurvivalRating SurvivalRating
    )
    {
        public static SimRatings FromYamlFile(string yamlFileName) => YamlReader.FromYamlFile<SimRatings>(yamlFileName);

        public IReadOnlyList<ISimRating> AvailableRatings()
        {
            return new List<ISimRating?> 
            {
               SurvivalRating,
            }
            .Where(r => r != null)
            .ToList()
            .AsReadOnly()!;
        }
    }

    /// <summary>
    /// Rates a simulation result based on survivability.
    /// </summary>
    public record SurvivalRating(double Lower, double Upper) : ISimRating
    {
        string ISimRating.Name => nameof(SurvivalRating);

        SimScore10 ISimRating.Score(SimResult result)
        {
            if (Lower >= Upper) throw new Exception("SurvivalRating - Invalid score range - Lower sould be < Upper.");

            // Concept:
            // 98% = Score 10 - Anything above 98% is not material
            // 80% = Scire 0  - Anything below is not material
            var survivalRate = result.SurvivalRate;

            if (survivalRate <= Lower) return new(0);
            if (survivalRate >= Upper) return new(10);

            var score = (survivalRate - Lower) / (Upper - Lower) * 10;
            return new SimScore10(Math.Round(score, 1));
        }

        public override string ToString() => $"SurvivalRating: [0..10] ~= [{Lower:P0}..{Upper:P0}]";
    }


}
