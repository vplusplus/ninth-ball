

namespace NinthBall.Reports
{
    static class Print
    {
        static readonly string DASHES = new('-', 90);
        public static void See(string substance, string fileName)
        {
            Console.WriteLine($" {substance,-20} | See {fileName}");
        }
    }
}
