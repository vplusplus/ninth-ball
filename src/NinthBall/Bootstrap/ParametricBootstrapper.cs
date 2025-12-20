
namespace NinthBall
{
    internal sealed class ParametricBootstrapper() : IBootstrapper
    {
        // We can produce theoretically unlimited possible combinations.
        public int GetMaxIterations(int numYears) => int.MaxValue;

        public IReadOnlyList<HROI> GetROISequence(int iterationIndex, int numYears)
        {
            throw new NotImplementedException("ParametricBootstrapper not implemented");
        }
    }
}
