
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
            IReadOnlyList<Percentile>? Percentiles = null,
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
            var htmlCols    = fromYaml?.Views?.TryGetValue(fromYaml.HtmlView  ?? string.Empty, out var userDefinedColumns) == true ? userDefinedColumns : null;
            var excelCols   = fromYaml?.Views?.TryGetValue(fromYaml.ExcelView ?? string.Empty, out userDefinedColumns) == true ? userDefinedColumns : null;

            return new
            (
                Percentiles:  null != percentiles && percentiles.Count > 0 ? percentiles : DefaultPercentiles,
                HtmlColumns:  null != htmlCols && htmlCols.Count > 0 ? htmlCols : DefaultColumns,
                ExcelColumns: null != excelCols && excelCols.Count > 0 ? excelCols : DefaultColumns
            );
        }

        // Default percentiles presented if user had not configured one.
        // NOTE: Public for now since Excel output is not yet refactord to the generalized approach.
        // NOTE: Make it private once Excel generation is refactored.
        public static IReadOnlyList<Percentile> DefaultPercentiles =>
        [
            new(0.01, "Worst-case"),
            new(0.05, "Unlucky"),
            new(0.10, "Unfortunate"),
            new(0.20, "Target"),
            new(0.50, "Coin-flip"),
            new(0.80, "Fortunate"),
            // new(0.90, "Lucky"),
            // new(0.95, "Dream"),
        ];

        // Default columns presented if user had not configured one.
        private static readonly IReadOnlyList<CID> DefaultColumns =
        [
            CID.Year, CID.Age,
            // Jan balance (ignore cash)
            CID.JanPreTax, CID.JanPostTax,
            // Additional incomes
            CID.SS, CID.Ann,
            // Expenses
            CID.Fees, CID.PYTaxes, CID.LivExp,
            // Net exchange from assets
            CID.XPreTax, CID.XPostTax,
            // Key market performance indicators 
            CID.LikeYear, CID.ROIStocks, CID.ROIBonds,
            // Bottom line - Approx asset value on year end.
            CID.AnnROI, CID.DecValue,
        ];

    }
}
