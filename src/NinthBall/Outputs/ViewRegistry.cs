
using NinthBall.Core;

namespace NinthBall.Outputs
{
    internal sealed class ViewRegistry(OutputDefaults Defaults, IReadOnlyDictionary<string, IReadOnlyList<CID>> CustomViews)
    {
        public IReadOnlyList<CID> ResolveView(string viewName)
        {
            ArgumentNullException.ThrowIfNull(viewName);

            return 
            (
                CustomViews.TryGetValue(viewName, out var view) && null != view && view.Count > 0 ||
                Defaults.Views.TryGetValue(viewName, out view) && null != view && view.Count > 0
            )
            ? view
            : throw new FatalWarning($"View not defined | '{viewName}'");
        }
    }
}
