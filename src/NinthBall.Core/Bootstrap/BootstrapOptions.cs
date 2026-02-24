
using System.ComponentModel.DataAnnotations;

namespace NinthBall.Core
{
    // Configuration options for the MBB internals
    public sealed record BootstrapOptions
    (
        [property: Required] 
        IReadOnlyList<int> BlockSizes,

        [property: Range(0.0, 1.0)] 
        double RegimeAwareness
    );
}
