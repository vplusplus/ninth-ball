using System.Text.Json;

namespace NinthBall
{
    internal class PCT2DoubleConverter : System.Text.Json.Serialization.JsonConverter<double>
    {
        public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                return reader.GetDouble();
            }
            else if (reader.TokenType == JsonTokenType.String)
            {
                string? strValue = reader.GetString();

                if (string.IsNullOrWhiteSpace(strValue))
                {
                    throw new Exception("Cannot convert empty string to double.");
                }
                else if (strValue.EndsWith('%'))
                {
                    var pctValue = strValue[0..^1].Trim();
                    return double.TryParse(pctValue, out var dblValue) ? dblValue / 100.0 : throw new Exception($"Given value '{strValue}' is not a valid percentage.");
                }
                else if (double.TryParse(strValue, out double dblValue))
                { 
                    return dblValue;
                }
                else
                {
                    throw new Exception($"Cannot convert '{strValue}' to double.");
                }
            }
            else
            {
                throw new Exception($"Unexpected token type '{reader.TokenType}' | Expecting a number or string");
            }
        }

        public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options) => writer.WriteNumberValue(value);
    }
}
