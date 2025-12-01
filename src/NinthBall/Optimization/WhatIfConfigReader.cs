

namespace NinthBall
{
    /// <summary>
    /// Reads WhatIfConfig from WhatIf.yaml file.
    /// </summary>
    public static class WhatIfConfigReader
    {
        public static WhatIfConfig Read(string yamlFileName) => YamlReader.ReadYamlFile<WhatIfConfig>(yamlFileName);
    }
}
