

using NinthBall.Utils;

namespace NinthBall
{
    static class Program
    {
        static async Task Main(string[] args)
        {
            Print.Header();
            try
            {
                await App.RunAsync();
            }
            catch (Exception unhandledException)
            {
                Print.ErrorSummaryAndDetails(unhandledException);
            }
        }
    }
}
