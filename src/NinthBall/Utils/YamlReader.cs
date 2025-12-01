
using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NinthBall
{
    internal static class YamlReader
    {
        public static T ReadYamlFile<T>(string yamlFileName)
        {
            ArgumentNullException.ThrowIfNull(yamlFileName);

            var myType = typeof(T).Name;
            var justFileName = Path.GetFileName(yamlFileName);

            if (!File.Exists(yamlFileName)) throw new FatalWarning($"Error reading {myType} | {justFileName} | File not found.");

            try
            {
                string yamlText = File.ReadAllText(yamlFileName);
                return ReadYamlText<T>(yamlText);

            }
            catch (Exception err)
            {
                throw new FatalWarning($"Error reading {myType} | {justFileName} | {err.Message}");
            }
        }

        public static T ReadYamlText<T>(string yamlText)
        {
            var myType = typeof(T).Name;

            try
            {
                // Deserialize as Yaml object
                object yamlObject = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .WithAttemptingUnquotedStringTypeDeserialization()
                    .Build()
                    .Deserialize(yamlText) ?? throw new Exception("Yaml.NET deserializer returned null.");

                // Convert to Json
                string jsonText = JsonSerializer.Serialize(yamlObject);

                // Deserialize from Json
                return JsonSerializer.Deserialize<T>(jsonText) ?? throw new Exception("Unexpected: JsonSerializer returned null.");
            }
            catch (Exception err)
            {
                throw new FatalWarning($"Error reading {myType} | {err.Message}");
            }
        }
    }
}

