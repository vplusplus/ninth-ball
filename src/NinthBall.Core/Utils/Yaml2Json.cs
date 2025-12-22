
using YamlDotNet.Serialization;

namespace NinthBall.Core
{
    internal static class Yaml2Json
    {
        public static string YamlTextToJsonText(string yamlInput)
        {
            ArgumentNullException.ThrowIfNull(yamlInput);

            var yamlDeserializer = new DeserializerBuilder().WithAttemptingUnquotedStringTypeDeserialization().Build();
            var yamlObject = yamlDeserializer.Deserialize<object>(new StringReader(yamlInput));
            var jsonText = System.Text.Json.JsonSerializer.Serialize(yamlObject, options: new() { WriteIndented = true });

            return jsonText;
        }
    }
}
