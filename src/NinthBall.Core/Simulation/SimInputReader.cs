
namespace NinthBall.Core
{
    public static class SimInputReader
    {
        public static SimInput ReadFromYamlFile(string yamlFileName)
        {
            const string MyPathTag = "$(MyPath)";

            var yamlText = File.ReadAllText(yamlFileName);

            // Replace $(MyPath)
            if (yamlText.Contains(MyPathTag, StringComparison.OrdinalIgnoreCase))
            {
                var myPath = Path.GetFullPath(Path.GetDirectoryName(yamlFileName) ?? "./")
                    .Replace('\\', '/')
                    .TrimEnd('/');

                yamlText = yamlText.Replace(MyPathTag, myPath, StringComparison.OrdinalIgnoreCase);
            }

            var jsonText = Yaml2Jaon.YamlTextToJsonText(yamlText);

            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
            };

            options.Converters.Add(new PercentageToDoubleConverter());
            options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

            return System.Text.Json.JsonSerializer.Deserialize<SimInput>(jsonText, options)
                ?? throw new Exception("Failed to deserialize SimConfig.");
        }
    }
}
