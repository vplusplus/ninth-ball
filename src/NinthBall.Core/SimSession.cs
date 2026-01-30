using Microsoft.Extensions.DependencyInjection;

namespace NinthBall.Core
{
    public interface ISimSession
    {
        Task<SimResult> RunAsync();
    }

    sealed class SimSession(IServiceProvider services) : ISimSession
    {
        // WHY:
        // Do not resolve Simulation using ctor injection. 
        // This will trigger a ripple-effect of exposing many components as public as agsinst internals.

        async Task<SimResult> ISimSession.RunAsync()
        {
            // WHY:
            // CPU driven, no IO. Runs synchronous.
            return services.GetRequiredService<Simulation>().RunSimulation();
        }
    }
}
