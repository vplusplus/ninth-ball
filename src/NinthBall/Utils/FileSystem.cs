
namespace NinthBall.Utils
{
    static class FileSystem
    {
        public static void EnsureDirectoryForFile(string filePath)
        {
            var dir = Path.GetDirectoryName(filePath) ?? "./";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }
    }
}
