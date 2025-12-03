
namespace NinthBall
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

        public static void PrintParams(this SimConfig simConfig)
        {
            var init = $"{simConfig.InitialBalance:C0}";
            var aloc = $"{simConfig.StockAllocation:P0}-{1-simConfig.StockAllocation:P0}";
            var year = $"{simConfig.NoOfYears}";
            var iter = $"{simConfig.Iterations:#,0}";

            Inform($"{init} | {aloc} | {year} years | {iter} iterations.");
        }

        public static void Footer(SimResult simResult, TimeSpan elapsed, string outputFileName) 
        {
            var survivalRate = simResult.SurvivalRate;
            var txtSurvivalRate = survivalRate > 0.99 ? $"{survivalRate:P1}" : $"{survivalRate:P0}";

            Inform($"{txtSurvivalRate} survival | {elapsed.TotalMilliseconds:#,0} mSec | See {Path.GetFileName(outputFileName)}");
        }

        static void Inform(string something) => Console.WriteLine($" [{DateTime.Now:HH\\:mm\\:ss}] {something}");

        public static void Error(Exception err)
        {
            Console.WriteLine();
            Console.WriteLine("An error occured:");
            Console.WriteLine(err);
        }
    }
}
