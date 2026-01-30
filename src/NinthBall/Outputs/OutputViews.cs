
using Microsoft.Extensions.Configuration;
using NinthBall.Core;

namespace NinthBall.Outputs
{
    internal sealed class OutputViews(IConfiguration Config, OutputDefaults Defaults)
    {
        static readonly IReadOnlyDictionary<string, IReadOnlyList<CID>> Empty = new Dictionary<string, IReadOnlyList<CID>>(0);

        readonly IReadOnlyDictionary<string, IReadOnlyList<CID>> CustomViews = Config.GetSection("Views").Get<IReadOnlyDictionary<string, IReadOnlyList<CID>>>() ?? Empty;

        readonly IReadOnlyDictionary<string, IReadOnlyList<CID>> DefaultViews = Defaults.Views ?? Empty;

        public IReadOnlyList<CID> ResolveView(string viewName)
        {
            ArgumentNullException.ThrowIfNull(viewName);

            return 
            (
                (CustomViews.TryGetValue(viewName, out var columns) && null != columns && columns.Count > 0) ||
                (DefaultViews.TryGetValue(viewName, out columns)    && null != columns && columns.Count > 0)
            )
            ? columns
            : throw new FatalWarning($"View not defined | '{viewName}'");
        }
    }
}
