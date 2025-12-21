
namespace NinthBall
{
    internal interface IBootstrapper
    {
        int GetMaxIterations(int numYears);
        IReadOnlyList<HROI> GetROISequence(int iterationIndex, int numYears);
    }
}
