
using System.Data;
using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NinthBall
{
    public static class SimConfigReader
    {
        public static SimConfig Read(string yamlFileName)
        {
            const string MyPathTag = "$(MyPath)";

            try
            {
                // Read YAML text
                string yamlText = File.ReadAllText(yamlFileName);

                // $(MyPath) represents path of the current config file.
                // If present, replace with path of the config file.
                if (yamlText.Contains(MyPathTag, StringComparison.OrdinalIgnoreCase))
                {
                    var myPath = Path.GetFullPath(Path.GetDirectoryName(yamlFileName) ?? "./")
                        .Replace('\\', '/')
                        .TrimEnd('/');

                    yamlText = yamlText.Replace(MyPathTag, myPath, StringComparison.OrdinalIgnoreCase);
                }

                // Deserialize as Yaml object
                object yamlObject = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .WithAttemptingUnquotedStringTypeDeserialization() // Helps recognize types like booleans and numbers implicitly
                    .Build()
                    .Deserialize(yamlText) ?? throw new Exception("Yaml.NET Deserializer returnd null.");

                // Convert to Json (because deserialization behaviors are different)
                string jsonText = JsonSerializer.Serialize(yamlObject);

                // Deserialize from Json 
                return System.Text.Json.JsonSerializer.Deserialize<SimConfig>(jsonText) ?? throw new Exception("Unexpected: JsonSerializer.Deserialize returned null.");
            }
            catch (Exception err)
            {
                throw new Exception($"Error reading SimConfig | {Path.GetFileName(yamlFileName)}", err);
            }
        }
    }
}

