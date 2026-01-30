using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NinthBall.Reports.Html
{
    internal static class HtmlTemplates
    {
        public static async Task<string> RenderTemplateAsync<TTemplate>(IServiceProvider services, IDictionary<string, object?> templateParameters) where TTemplate : IComponent
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(templateParameters);

            var loggerFactory = services.GetRequiredService<ILoggerFactory>();
            await using var htmlRenderer = new HtmlRenderer(services, loggerFactory);

            // Entire rendering flow stays within the htmlRenderer.Dispatcher
            return await htmlRenderer.Dispatcher.InvokeAsync(async () =>
            {
                // Do NOT use ConfigureAwait(false) here; stay on the Dispatcher
                var parameters = templateParameters.Count > 0 ? ParameterView.FromDictionary(templateParameters) : ParameterView.Empty;
                var output = await htmlRenderer.RenderComponentAsync<TTemplate>(parameters);

                return output.ToHtmlString();
            });
        }
    }
}
