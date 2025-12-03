
using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NinthBall
{
    internal static class YamlReader
    {
        public static T FromYamlFile<T>(string yamlFileName) => File
            .ReadAllText(yamlFileName)
            .YamlTextToJaonText()
            .DeserializeJsonText<T>();

        public static T FromYamlText<T>(string yamlText) => yamlText
            .YamlTextToJaonText()
            .DeserializeJsonText<T>();

        static string YamlTextToJaonText(this string yamlText)
        {
            ArgumentNullException.ThrowIfNull(yamlText);

            try
            {
                // Deserialize as Yaml object
                object yamlObject = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .WithAttemptingUnquotedStringTypeDeserialization()
                    .Build()
                    .Deserialize(yamlText) ?? throw new Exception("Yaml.NET deserializer returned null.");

                // Convert to Json
                return System.Text.Json.JsonSerializer.Serialize(yamlObject);
            }
            catch (Exception err)
            {
                throw new Exception($"Error convertng YAML to Json.", err);
            }
        }

        static T DeserializeJsonText<T>(this string jsonText)
        {
            var options = new JsonSerializerOptions()
            {
                WriteIndented = true,
            };
            options.Converters.Add(new PCT2DoubleConverter());

            // Deserialize from Json
            return JsonSerializer.Deserialize<T>(jsonText, options) ?? throw new Exception("Unexpected: JsonSerializer returned null.");
        }
    }
}

