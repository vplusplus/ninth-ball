
using System.Globalization;
using System.Text.Json.Nodes;

namespace NinthBall.Core
{
    internal static class JsonNodeExtensions
    {
        /// <summary>
        /// Cosmetic fluent helper to translate a json text to JsonObject
        /// </summary>
        public static JsonObject AsJsonObject(this string jsonContent) => string.IsNullOrWhiteSpace(jsonContent)  ? [] : JsonNode.Parse(jsonContent) as JsonObject ?? [];

        /// <summary>
        /// Can replace string formatted numbers ($1,000) and percentage values (60%) to double.
        /// </summary>
        public static JsonNode PatchNumbersAndPercentage(this JsonNode node)
        {
            if (null != node) VisitNode(node);
            return node!;

            static void VisitNode(JsonNode node)
            {
                if (null == node) return;
                else if (node is JsonObject obj) VisitObject(obj);
                else if (node is JsonArray arr) VisitArray(arr);
            }

            static void VisitObject(JsonObject obj)
            {
                foreach (var property in obj)
                {
                    if (null == property.Value) continue;

                    if (property.Value is JsonValue oldValue)
                    {
                        var replace = TryPatchNumber(oldValue, out var newValue);
                        if (replace) obj[property.Key] = newValue;
                    }
                    else
                    {
                        VisitNode(property.Value);
                    }
                }
            }

            static void VisitArray(JsonArray arr)
            {
                for (int i = 0; i < arr.Count; i++)
                {
                    if (null == arr[i]) continue;

                    if (arr[i] is JsonValue oldValue)
                    {
                        var replace = TryPatchNumber(oldValue, out var newValue);
                        if (replace) arr[i] = newValue;
                    }
                    else
                    {
                        VisitNode(arr[i]!);
                    }
                }
            }

            static bool TryPatchNumber(JsonValue oldValue, out JsonValue newValue)
            {
                newValue = oldValue;

                if (oldValue.TryGetValue<string>(out var str))
                {
                    str = str.Trim();

                    // Handle percentage strings
                    if (str.EndsWith("%") && double.TryParse(str[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var pct))
                    {
                        newValue = JsonValue.Create(pct / 100.0);
                        return true;
                    }

                    // Ignore optional $ prefix if present
                    if (str.StartsWith("$")) str = str[1..].Trim();

                    // Try parse number
                    if (double.TryParse(str, out var num))
                    {
                        newValue = JsonValue.Create(num);
                        return true;
                    }
                }

                return false;
            }
        }

    }
}
