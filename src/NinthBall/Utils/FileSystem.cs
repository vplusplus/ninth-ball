using DocumentFormat.OpenXml.Math;
using System;
using System.Collections.Generic;
using System.Text;

namespace NinthBall
{
    internal static class FileSystem
    {
        public static void EnsureDirectoryForFile(string filePath)
        {
            var dir = Path.GetDirectoryName(filePath) ?? "./";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }
    }
}
