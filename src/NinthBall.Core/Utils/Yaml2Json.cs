
namespace NinthBall.Utils
{
    static class Yaml2Json
    {
        public static string YamlTextToJsonText(string yamlInput)
        {
            ArgumentNullException.ThrowIfNull(yamlInput);

            try
            {
                var yamlDeserializer = new YamlDotNet.Serialization.DeserializerBuilder().WithAttemptingUnquotedStringTypeDeserialization().Build();
                var yamlObject = yamlDeserializer.Deserialize<object>(new StringReader(yamlInput));
                var jsonText = System.Text.Json.JsonSerializer.Serialize(yamlObject, options: new() { WriteIndented = true });
                return jsonText;
            }
            catch (Exception ex) 
            {
                throw new Exception("Error converting YAML text to Json text.", ex);
            }
        }
    }
}
