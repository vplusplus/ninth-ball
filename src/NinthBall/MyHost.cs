
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NinthBall.Core;

namespace NinthBall.Hosting
{
    internal static class MyHost
    {
        public static IHost DefineMyApp()
        {
            var appBuilder = Host.CreateEmptyApplicationBuilder(settings: new());

            appBuilder.Configuration.AddConfiguration(CmdLine.Current);

            appBuilder.Services
                .AddSingleton<SimRunner>()
                .AddTransient<App>()
                ;
            return appBuilder.Build();
        }

    }
}
