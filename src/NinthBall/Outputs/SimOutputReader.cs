
using NinthBall.Core;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NinthBall.Outputs
{
    internal static class SimOutputReader
    {
        //  Represents raw data from yaml configuration.
        internal record SimOutputYaml
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
        internal static SimOutput TryLoad(string yamlFileName) => ReadFromYamlFile(yamlFileName).ToSimOutput();

        public static SimOutputYaml ReadFromYamlFile(string yamlFileName)
        {
            if (!File.Exists(yamlFileName)) return new();

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

            return JsonSerializer.Deserialize<SimOutputYaml>(jsonText, options) ?? new();
        }

        // Translates SimOutputYaml to null-free configuration with defaults where required. 
        public static SimOutput ToSimOutput(this SimOutputYaml? fromYaml)
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
