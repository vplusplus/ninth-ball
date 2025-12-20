
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace NinthBall.Hosting
{
    /// <summary>
    /// Extension methods for adding json-friendly-options configuration to the DI container.
    /// </summary>
    internal static class JsonFriendlyOptionsConfigurationExtensions
    {
        /// <summary>
        /// Binds the config section as TOptions.
        /// Uses System.Text.Json to deserialize TOptions, with support for numbers with formatting represented as strings and percentage values.
        /// </summary>
        public static TOptions GetEx<TOptions>(this IConfiguration configSection)
        {
            ArgumentNullException.ThrowIfNull(configSection);

            // Transcribe the config section to json
            var configSectionAsJsonNode = ConvertToJsonNode(configSection);

            // Accept numbers represented as string (Example: "1,000,000")
            // Accept percentage values (Example: 60%)
            var options = new JsonSerializerOptions() { NumberHandling = JsonNumberHandling.AllowReadingFromString };
            options.Converters.Add(new PercentageToDoubleConverter());

            // Deserialize using System.Text.Json
            return configSectionAsJsonNode.Deserialize<TOptions>(options) ?? throw new Exception($"Failed to deserialize config section | {typeof(TOptions).Name}");
        }

        /// <summary>
        /// Supports Json serializer.
        /// Can convert PCT values (example: 60%) to double
        /// </summary>
        private sealed class PercentageToDoubleConverter : System.Text.Json.Serialization.JsonConverter<double>
        {
            public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
                reader.TokenType == JsonTokenType.Number ? reader.GetDouble() :
                reader.TokenType == JsonTokenType.String ? ParseDoubleOrPercentage(reader.GetString()) :
                throw new Exception($"Unexpected token type '{reader.TokenType}' | Expecting a number or string");

            public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options) =>
                writer.WriteNumberValue(value);
        }

        /// <summary>
        /// Returns json representation of a config section.
        /// </summary>
        private static JsonNode ConvertToJsonNode(IConfiguration configSection, int nestingLevel = 0)
        {
            const int MAX_NESTING_LEVEL = 25;

            // The Circuit Breaker Check:
            if (nestingLevel > MAX_NESTING_LEVEL) throw new Exception($"IConfiguration.ConvertToJsonNode() detected more than {MAX_NESTING_LEVEL} descendant levels. Processing stopped by design.");

            if (null == configSection) return new JsonObject();

            if (configSection.GetChildren().Any())
            {
                var isAnArray = configSection.GetChildren().All(c => int.TryParse(c.Key, out _));

                if (isAnArray)
                {
                    JsonArray arr = new();
                    foreach (var child in configSection.GetChildren()) if (null != child) arr.Add(ConvertToJsonNode(child, nestingLevel + 1));
                    return arr;
                }
                else
                {
                    JsonObject obj = new();
                    foreach (var child in configSection.GetChildren()) if (null != child) obj.Add(child.Key, ConvertToJsonNode(child, nestingLevel + 1));
                    return obj;
                }
            }
            else
            {
                var txtValue = (configSection as IConfigurationSection)?.Value;
                return ToJsonValue(txtValue);
            }

            // Because, IConfiguration manages all values as strings...
            static JsonValue ToJsonValue(string? txt) =>
                string.IsNullOrWhiteSpace(txt) ? null! :
                bool.TryParse(txt, out var boolValue) ? JsonValue.Create(boolValue) :
                double.TryParse(txt, out var dblValue) ? JsonValue.Create(dblValue) :
                long.TryParse(txt, out var longValue) ? JsonValue.Create(longValue) :
                JsonValue.Create(txt);
        }

        /// <summary>
        /// Can parse numbers represented as a fractional value (0.6) or percentage (60%)
        /// </summary>
        private static double ParseDoubleOrPercentage(string? something)
        {
            if (string.IsNullOrWhiteSpace(something)) return 0.0;
            else if (double.TryParse(something, out var dblValue)) return dblValue;
            else if (something.EndsWith('%') && something.Length > 1 && double.TryParse(something[..^1].Trim(), out dblValue)) return dblValue / 100.0;
            else throw new Exception($"Cannot convert '{something}' to double value.");
        }
    }
}
