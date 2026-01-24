
using NinthBall.Core;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NinthBall.Outputs
{
    internal static class SimOutputReader
    {
        //  Represents raw data from yaml configuration.
        private record SimOutputYaml
        (
            IReadOnlyList<double>? Percentiles = null,
            IReadOnlyList<int>? Iterations = null,
            IReadOnlyDictionary<string, IReadOnlyList<CID>>? Views = null,
            string? HtmlView = null,
            string? ExcelView = null
        );

        /// <summary>
        /// Reads SimOutput.yaml - Locates the file based on conventions.
        /// </summary>
        internal static SimOutput TryLoad(string simInputFileName)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(simInputFileName);

            // Use the input file name directory as reasonable starting point
            var dir = Path.GetDirectoryName(simInputFileName) ?? "./";

            // Convention based output configuration file names.
            string[] possibleSimOutputYamlFileNames = 
            [
                Path.Combine(dir, Path.GetFileNameWithoutExtension(simInputFileName) + ".output.yaml"),
                Path.Combine(dir,  "SimOutput.yaml"),
                Path.Combine("./", "SimOutput.yaml"),
            ];

            foreach (var yamlFileName in possibleSimOutputYamlFileNames)
            {
                if (File.Exists(yamlFileName))
                {
                    Console.WriteLine($" Using {Path.GetFullPath(yamlFileName)}");
                    return ReadFromYamlFile(yamlFileName).ToSimOutput();
                }
            }

            Console.WriteLine($" Using all defaults for simulation output configurations.");
            return new SimOutputYaml().ToSimOutput();
        }

        static SimOutputYaml? ReadFromYamlFile(string yamlFileName)
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

            return JsonSerializer.Deserialize<SimOutputYaml>(jsonText, options);
        }

        // Translates SimOutputYaml to null-free configuration with defaults where required. 
        private static SimOutput ToSimOutput(this SimOutputYaml? fromYaml)
        {
            var percentiles = fromYaml?.Percentiles;
            var iterations  = fromYaml?.Iterations;
            var htmlCols    = fromYaml?.Views?.TryGetValue(fromYaml.HtmlView  ?? string.Empty, out var userDefinedColumns) == true ? userDefinedColumns : null;
            var excelCols   = fromYaml?.Views?.TryGetValue(fromYaml.ExcelView ?? string.Empty, out userDefinedColumns) == true ? userDefinedColumns : null;

            return new
            (
                Percentiles:  null != percentiles && percentiles.Count > 0 ? percentiles : SimOutputDefaults.DefaultPercentiles,
                Iterations: null != iterations ? iterations : [],
                HtmlColumns:  null != htmlCols && htmlCols.Count > 0 ? htmlCols : SimOutputDefaults.DefaultColumns,
                ExcelColumns: null != excelCols && excelCols.Count > 0 ? excelCols : SimOutputDefaults.DefaultColumns
            );
        }
    }
}
