
namespace NinthBall
{
    public static class SimConfigReader
    {
        public static SimConfig Read(string yamlFileName)
        {
            const string MyPathTag = "$(MyPath)";

            try
            {
                // ReadYamlFile YAML text
                string yamlText = File.ReadAllText(yamlFileName);

                // The $(MyPath) token represents directory of the current config file.
                // Config entries can reference $(MyPath) to locate related files.
                // If present, replace $(MyPath) with the actual path of this config file.
                if (yamlText.Contains(MyPathTag, StringComparison.OrdinalIgnoreCase))
                {
                    var myPath = Path.GetFullPath(Path.GetDirectoryName(yamlFileName) ?? "./")
                        .Replace('\\', '/')
                        .TrimEnd('/');

                    yamlText = yamlText.Replace(MyPathTag, myPath, StringComparison.OrdinalIgnoreCase);
                }

                return YamlReader.ReadYamlText<SimConfig>(yamlText);
            }
            catch (Exception err)
            {
                throw new Exception($"Error reading SimConfig | {Path.GetFileName(yamlFileName)}", err);
            }
        }
    }
}

