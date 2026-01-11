
using NinthBall.Core;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NinthBall.Outputs
{
    internal static class SimOutputReader
    {
        /// <summary>
        /// Looks for a SimOutput.yaml file in the same directory as the input file.
        /// First looks for {simInputFileName}.output.yaml, then SimOutput.yaml
        /// </summary>
        internal static SimOutput? TryLoad(string simInputFileName)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(simInputFileName);

            // Use the input file name directory as a proxy
            var dir = Path.GetDirectoryName(simInputFileName) ?? "./";

            string[] possibleOutputFileNames = 
            [
                Path.Combine(dir, Path.GetFileNameWithoutExtension(simInputFileName) + ".output.yaml"),
                Path.Combine(dir,  "SimOutput.yaml"),
                Path.Combine("./", "SimOutput.yaml"),
            ];

            foreach (var yamlFileName in possibleOutputFileNames)
            {
                if (File.Exists(yamlFileName))
                {
                    Console.WriteLine($" Using {Path.GetFullPath(yamlFileName)}");
                    return ReadFromYamlFile(yamlFileName);
                }
            }

            return null;
        }

        static SimOutput? ReadFromYamlFile(string yamlFileName)
        {
            if (!File.Exists(yamlFileName)) return null;

            var yamlText = File.ReadAllText(yamlFileName);
            var jsonText = Yaml2Json.YamlTextToJsonText(yamlText);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            };

            options.Converters.Add(new PercentageToDoubleConverter());
            options.Converters.Add(new JsonStringEnumConverter());

            return JsonSerializer.Deserialize<SimOutput>(jsonText, options);
        }
    }
}
