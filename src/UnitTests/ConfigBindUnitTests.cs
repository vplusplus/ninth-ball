using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NinthBall.Core;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;

namespace UnitTests
{
    [TestClass]
    public class ConfigBindUnitTests
    {
        [TestMethod]
        public void HelloConfig()
        {
            var appBuilder = Host.CreateEmptyApplicationBuilder(settings: new());

            appBuilder.Configuration
                .AddYamlResource(typeof(ConfigBindUnitTests).Assembly, "Sample.yaml")
                .Build();

            appBuilder.Services
                .RegisterConfigSection<InitialBalance>()
                ;

            var app = appBuilder.Build();

            // var c1 = app.Services.GetRequiredService<InitialBalance>();
            // Console.WriteLine(c1);

            var c2 = app.Services.GetRequiredService<Lazy<InitialBalance>>();
            Console.WriteLine(null == c2.Value ? "NULL" : c2.Value);
        }
    }




    public static class ConfigurationExtensions
    {
        public static IConfigurationBuilder AddYamlFile(this IConfigurationBuilder builder, string yamlFileName)
        {
            var yamlText = File.ReadAllText(yamlFileName);
            return builder.AddYamlFile(yamlText);
        }

        public static IConfigurationBuilder AddYamlResource(this IConfigurationBuilder builder, Assembly resourceAssembly, string resourceNameEndsWith)
        {
            var resourceName = resourceAssembly.GetManifestResourceNames().SingleOrDefault(x => x.EndsWith(resourceNameEndsWith, StringComparison.OrdinalIgnoreCase))
                ?? throw new Exception($"Resource not found | Assembly: {resourceAssembly.GetName().Name} | Resource: {resourceNameEndsWith}");

            using var resStream = resourceAssembly.GetManifestResourceStream(resourceName)
                ?? throw new Exception($"Resource stream was NULL | Assembly: {resourceAssembly.GetName().Name} | Resource: {resourceNameEndsWith}");

            using var reader = new StreamReader(resStream);
            var yamlText = reader.ReadToEnd();
            return builder.AddYamlContent(yamlText);
        }

        static IConfigurationBuilder AddYamlContent(this IConfigurationBuilder builder, string yamlContent)
        {
            var jsonContent = Yaml2Json.YamlTextToJsonText(yamlContent);
            var jsonNode = JsonNode.Parse(jsonContent) ?? new JsonObject();
            PatchNumbersAndPercentage(jsonNode);
            jsonContent = jsonNode!.ToJsonString();

            var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(jsonContent));
            return builder.AddJsonStream(jsonStream);

            static JsonNode PatchNumbersAndPercentage(JsonNode node)
            {
                if (null != node) VisitNode(node);
                return node!;

                static void VisitNode(JsonNode node)
                {
                    if (null == node) return;
                    else if (node is JsonObject obj) VisitObject(obj);
                    else if (node is JsonArray arr) VisitArray(arr);
                }

                static void VisitObject(JsonObject obj)
                {
                    foreach (var property in obj)
                    {
                        if (null == property.Value) continue;

                        if (property.Value is JsonValue oldValue)
                        {
                            var replace = TryPatchNumber(oldValue, out var newValue);
                            if (replace) obj[property.Key] = newValue;
                        }
                        else
                        {
                            VisitNode(property.Value);
                        }
                    }
                }

                static void VisitArray(JsonArray arr)
                {
                    for (int i = 0; i < arr.Count; i++)
                    {
                        if (null == arr[i]) continue;

                        if (arr[i] is JsonValue oldValue)
                        {
                            var replace = TryPatchNumber(oldValue, out var newValue);
                            if (replace) arr[i] = newValue;
                        }
                        else
                        {
                            VisitNode(arr[i]!);
                        }
                    }
                }

                static bool TryPatchNumber(JsonValue oldValue, out JsonValue newValue)
                {
                    newValue = oldValue;

                    if (oldValue.TryGetValue<string>(out var str))
                    {
                        str = str.Trim();

                        // Handle percentage strings
                        if (str.EndsWith("%") && double.TryParse(str[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var pct))
                        {
                            newValue = JsonValue.Create(pct / 100.0);
                            return true;
                        }

                        // Ignore optional $ prefix if present
                        if (str.StartsWith("$")) str = str[1..].Trim();

                        // Try parse number
                        if (double.TryParse(str, out var num))
                        {
                            newValue = JsonValue.Create(num);
                            return true;
                        }
                    }

                    return false;
                }
            }

        }

        public static IServiceCollection RegisterConfigSection<TSection>(this IServiceCollection services) where TSection : class
        {
            var sectionName = typeof(TSection).Name;

            // Lazy option:
            // Returns null if section not defined.
            // Returns validated TSection is section is defined
            services.AddSingleton<Lazy<TSection>>(sp => new Lazy<TSection>(() =>
            {
                var configuration = sp.GetRequiredService<IConfiguration>();
                var configSection = configuration.GetSection(sectionName);
                var options = configSection.Exists() ? configSection.Get<TSection>() : null;
                return null != options ? Validate(options, sectionName) : null!;
            }));

            // Not so lazy option:
            // Throws an exception if section not defined.
            // Returns validated TSection is section is defined
            services.AddSingleton<TSection>(sp => sp.GetRequiredService<Lazy<TSection>>().Value ?? throw new Exception($"Missing config section | '{sectionName}'"));

            return services;

            static TSomething Validate<TSomething>(TSomething options, string sectionName) where TSomething : class
            {
                var context = new ValidationContext(options);
                var results = new List<ValidationResult>();

                if (!Validator.TryValidateObject(options, context, results, validateAllProperties: true))
                {
                    var messages = string.Join(Environment.NewLine, results.Select(x => x.ErrorMessage));
                    throw new ValidationException($"Invalid config section | '{sectionName}':{Environment.NewLine}{messages}");
                }

                return options;
            }
        }

    }

}




