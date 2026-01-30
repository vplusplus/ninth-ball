using Microsoft.Extensions.DependencyInjection;
using NinthBall.Core;
using NinthBall.Outputs;

namespace NinthBall.Outputs
{
    public interface ISimOutputSession
    {
        Task GenerateAsync(SimResult simResult);
    }

    internal sealed class SimOutputSession(IServiceProvider Services) : ISimOutputSession
    {
        // WHY:
        // Do not resolve SimReports using ctor injection. 
        // This will trigger a ripple-effect of exposing many components as public as agsinst internals.

        readonly SimReports SimReports = Services.GetRequiredService<SimReports>();

        async Task ISimOutputSession.GenerateAsync(SimResult simResults)
        {
            await Services.GetRequiredService<SimReports>().GenerateAsync(simResults);
        }
    }
}
