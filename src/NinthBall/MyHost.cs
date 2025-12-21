
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using YamlDotNet.Serialization;

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

        public static string YamlTextToJsonText(string yamlInput)
        {
            var yamlDeserializer = new DeserializerBuilder().WithAttemptingUnquotedStringTypeDeserialization().Build();
            var yamlObject = yamlDeserializer.Deserialize<object>(new StringReader(yamlInput));
            var jsonText = System.Text.Json.JsonSerializer.Serialize(yamlObject, options: new() { WriteIndented = true });

            return jsonText;
        }
    }
}
