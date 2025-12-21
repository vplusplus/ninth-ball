
using Microsoft.Extensions.DependencyInjection;
using NinthBall.Hosting;
using System.ComponentModel.DataAnnotations;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("UnitTests")]

namespace NinthBall
{
    internal static class Program
    {
        static async Task Main(string[] args)
        {
            Print.Header();

            try
            {
                await MyHost.DefineMyApp().Services.GetRequiredService<App>().RunAsync();
            }
            catch(FatalWarning warn)
            {
                Console.WriteLine(warn.Message);
            }
            catch (ValidationException validationErr)
            {
                Console.WriteLine(validationErr.Message);
            }
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());
            }
        }
    }
}
