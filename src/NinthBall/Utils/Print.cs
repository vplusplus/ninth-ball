
using NinthBall.Core;

namespace NinthBall.Utils
{
    internal static class Print
    {
        static readonly string DASHES = new('-', 90);

        public static void Header()
        {
            var me = typeof(Program).Assembly.GetName();

            Console.WriteLine();
            Console.WriteLine(DASHES);
            Console.WriteLine($" {me.Name} v{me.Version}  Now: {DateTime.Now}");
            Console.WriteLine(DASHES);
        }

        public static void Help()
        {
            var me = typeof(Program).Assembly.GetName().Name;

            var resourceName = typeof(App).Assembly.GetManifestResourceNames().Where(x => x.EndsWith("help.txt", StringComparison.OrdinalIgnoreCase)).Single();
            using var resStream = typeof(App).Assembly.GetManifestResourceStream(resourceName) ?? throw new Exception("Unexpected | Resource stream was null.");
            using var reader = new StreamReader(resStream);
            var helpText = reader.ReadToEnd();

            helpText = helpText.Replace("{me}", me, StringComparison.OrdinalIgnoreCase);
            Console.WriteLine(helpText);
        }

        public static void ErrorSummary(Exception err)
        {
            if (null == err) return;

            Console.WriteLine(RootCause(err));

            if (null != err.InnerException)
            {
                Console.WriteLine("Additional details:");
                while(null != err)
                {
                    Console.WriteLine(err.Message);
                    err = err.InnerException!;
                } 
            }

            static string RootCause(Exception ex)
            {
                string rootCause = string.Empty;
                while(null != ex)
                {
                    rootCause = ex.Message;
                    ex = ex.InnerException!;
                }
                return rootCause;
            }
        }

        public static void Done(SimResult simResult, TimeSpan elapsed) 
        {
            var survivalRate = simResult.SurvivalRate;
            var txtSurvivalRate = survivalRate > 0.99 ? $"{survivalRate:P1}" : $"{survivalRate:P0}";

            Console.WriteLine($" Done | {txtSurvivalRate} survival | {elapsed.TotalMilliseconds:#,0} mSec.");
        }
    }
}
