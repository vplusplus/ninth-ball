using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace NinthBall
{
    /// <summary>
    /// Supports Json serializer.
    /// Can convert PCT values (example: 60%) to double
    /// </summary>
    internal sealed class PercentageToDoubleConverter : System.Text.Json.Serialization.JsonConverter<double>
    {
        public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.TokenType == JsonTokenType.Number ? reader.GetDouble() :
            reader.TokenType == JsonTokenType.String ? ParseDoubleOrPercentage(reader.GetString()) :
            throw new Exception($"Unexpected token type '{reader.TokenType}' | Expecting a number or string");

        public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options) =>
            writer.WriteNumberValue(value);

        /// <summary>
        /// Can parse numbers represented as a fractional value (0.6) or percentage (60%)
        /// </summary>
        private static double ParseDoubleOrPercentage(string? something)
        {
            if (string.IsNullOrWhiteSpace(something)) return 0.0;
            else if (double.TryParse(something, out var dblValue)) return dblValue;
            else if (something.EndsWith('%') && something.Length > 1 && double.TryParse(something[..^1].Trim(), out dblValue)) return dblValue / 100.0;
            else throw new Exception($"Cannot convert '{something}' to double value.");
        }
    }


}
