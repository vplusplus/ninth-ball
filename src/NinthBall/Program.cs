

using NinthBall.Core;
using NinthBall.Utils;
using System.ComponentModel.DataAnnotations;

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
            catch (Exception warning) when (warning is FatalWarning or ValidationException)
            {
                // WHY: FatalWarning and ValidationException are information, not errors.
                Console.WriteLine(warning.Message);
            }
            catch (Exception unhandledException)
            {
                // User should never see this. If it happens, they see an ugly dump.
                Print.Error(unhandledException, includeStackTrace: true);
            }
        }
    }
}
