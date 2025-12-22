
namespace NinthBall.Core
{
    public static class SimInputReader
    {
        public static SimInput ReadFromYamlFile(string yamlFileName)
        {
            const string MyPathTag = "$(MyPath)";

            var yamlText = File.ReadAllText(yamlFileName);

            // YAML elements can refer to other related files using $(MyPath)
            // Replace $(MyPath) with the path of the YAML file.
            if (yamlText.Contains(MyPathTag, StringComparison.OrdinalIgnoreCase))
            {
                var myPath = Path.GetFullPath(Path.GetDirectoryName(yamlFileName) ?? "./")
                    .Replace('\\', '/')
                    .TrimEnd('/');

                yamlText = yamlText.Replace(MyPathTag, myPath, StringComparison.OrdinalIgnoreCase);
            }

            // Convert YAML to Json
            var jsonText = Yaml2Json.YamlTextToJsonText(yamlText);

            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
            };

            // We support opinionated conversions (like 60% = 0.6)
            // And we have some enums.
            options.Converters.Add(new PercentageToDoubleConverter());
            options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

            return System.Text.Json.JsonSerializer.Deserialize<SimInput>(jsonText, options)
                ?? throw new Exception($"Failed to deserialize {nameof(SimInput)} | JsonSerializer returned NULL.");
        }
    }
}
