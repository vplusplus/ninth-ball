
using YamlDotNet.Serialization;

namespace NinthBall.Core
{
    public static class Yaml2Jaon
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
