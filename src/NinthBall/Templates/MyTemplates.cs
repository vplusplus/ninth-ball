using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// REF: https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-components-outside-of-aspnetcore?view=aspnetcore-10.0

namespace NinthBall.Templates
{
    internal static class MyTemplates
    {
        static readonly Lazy<IServiceProvider> LazyServices = new(() => new ServiceCollection()
            .AddLogging()
            .BuildServiceProvider()
        );

        static async Task<string> RenderTemplateAsync<TTemplate>(IDictionary<string, object?> templateParameters = null!) where TTemplate : IComponent
        {
            using (var htmlRenderer = new HtmlRenderer(LazyServices.Value, LazyServices.Value.GetRequiredService<ILoggerFactory>()))
            {
                return await htmlRenderer.Dispatcher.InvokeAsync(async () =>
                {
                    var output = await htmlRenderer.RenderComponentAsync<TTemplate>(
                        ParameterView.FromDictionary(
                            templateParameters ?? new Dictionary<string, object?>()
                        )
                    );

                    return output.ToHtmlString();
                });
            }
        }

        public static async Task<string> GenerateSimReportAsync(SimResult simOutcome)
        {
            Dictionary<string, object?> templateParameters = new() 
            { 
                ["Model"] = simOutcome 
            };

            return await RenderTemplateAsync<SimReport>(templateParameters).ConfigureAwait(false);
        }

        public static async Task<string> GenerateErrorHtmlAsync(Exception err)
        {
            err = err ?? new Exception("Sorry, error object itself was null.");

            Dictionary<string, object?> templateParameters = new()
            {
                ["Ex"] = err
            };

            return await RenderTemplateAsync<Error>(templateParameters).ConfigureAwait(false);
        }
    }
}
