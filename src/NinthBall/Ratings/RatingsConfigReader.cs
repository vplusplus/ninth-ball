
namespace NinthBall
{
    /// <summary>
    /// Reads RatingsConfig from Ratings.yaml file.
    /// </summary>
    public static class RatingsConfigReader
    {
        public static RatingsConfig Read(string yamlFileName) => YamlReader.ReadYamlFile<RatingsConfig>(yamlFileName);
    }
}
