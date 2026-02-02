using System;
using System.Collections.Generic;
using System.Text;

namespace UnitTests
{
    [TestClass]
    public class CodeDump
    {

        [TestMethod]
        public void PrepCodeForAgents()
        {
            string BaseFolder = "D:\\Source\\ninth-ball\\src\\";
            string OutputFileName = @"D:/Junk/Source.md";

            string[] Extensions = [
                ".cs",
                ".yaml"
            ];


            string[] Exclude = [
                "UnitTests", 
                ".artifacts"
            ];

            var baseDir = new DirectoryInfo(Path.GetFullPath(BaseFolder));
            var allFiles = baseDir.GetFiles("*.*", SearchOption.AllDirectories).OrderBy(f => f.FullName).ToList();

            using(var writer = File.CreateText(OutputFileName))
            {
                foreach (var someFile in allFiles)
                {
                    var include = Extensions.Any(x => someFile.Name.EndsWith(x, StringComparison.OrdinalIgnoreCase));
                    if (!include) continue;

                    var exclude = Exclude.Any(x => someFile.FullName.Contains(x, StringComparison.OrdinalIgnoreCase));
                    if (exclude) continue;

                    var relativePath = someFile.FullName.Substring(baseDir.FullName.Length - 1);

                    writer.WriteLine();
                    writer.WriteLine($"# File: {relativePath}");
                    writer.WriteLine($"```csharp");
                    writer.WriteLine(File.ReadAllText(someFile.FullName));
                    writer.WriteLine($"```");
                    writer.WriteLine("---");
                }
            }
        }
    }
}
