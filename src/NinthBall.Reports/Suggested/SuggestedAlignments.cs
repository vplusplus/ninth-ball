
using static NinthBall.Reports.AlignHint;

namespace NinthBall.Reports
{
    internal enum AlignHint { Left, Center, Right }

    internal static partial class Suggested
    {
        internal static AlignHint GetAlignmentHint(this CID cid) => Alignments.TryGetValue(cid, out var alignHint) ? alignHint : AlignHint.Right;

        static readonly IReadOnlyDictionary<CID, AlignHint> Alignments = new Dictionary<CID, AlignHint>()
        {
            [CID.Year]      = Center,
            [CID.Age]       = Center,
            [CID.LikeYear]  = Center,

        }.AsReadOnly();

    }
}
