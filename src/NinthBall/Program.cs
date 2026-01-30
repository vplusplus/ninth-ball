
using System.ComponentModel.DataAnnotations;
using NinthBall.Core;
using NinthBall.Utils;

namespace NinthBall
{
    internal static class Program
    {
        static async Task Main(string[] args)
        {
            Print.Header();
            try
            {
                await App.RunAsync();
            }
            catch (Exception warning) when (warning is FatalWarning or ValidationException)
            {
                Console.WriteLine(warning.Message);
            }
            catch (Exception unhandledException)
            {
                Print.ErrorSummaryAndDetails(unhandledException);
            }
        }
    }
}
