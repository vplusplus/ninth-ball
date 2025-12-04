
namespace NinthBall
{
    /// <summary>
    /// Evaluates simulation result on a specific criterion.
    /// Returns absolute score [0.0 .. 1.0]
    /// </summary>
    public interface ISimRating
    {
        /// <summary> Name of the rating criterion for display and logging. </summary>
        string Name { get; }

        /// <summary>
        /// Scores the simulation result on an absolute scale.
        /// Returns 0.0 (unacceptable) to 1.0 (ideal) based on domain knowledge,
        /// </summary>
        SimScore10 Score(SimResult result);
    }
}
