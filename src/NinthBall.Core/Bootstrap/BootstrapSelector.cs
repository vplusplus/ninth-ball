
using Microsoft.Extensions.DependencyInjection;

namespace NinthBall.Core
{
    internal sealed class BootstrapSelector(IServiceProvider AvailableServices, Growth GrowthOptions)
    {
        public IBootstrapper GetSelectedBootstrapper()
        {
            return GrowthOptions.Bootstrapper switch
            {
                BootstrapKind.Flat          => AvailableServices.GetRequiredService<FlatBootstrapper>(),
                BootstrapKind.Sequential    => AvailableServices.GetRequiredService<SequentialBootstrapper>(),
                BootstrapKind.MovingBlock   => AvailableServices.GetRequiredService<MovingBlockBootstrapper>(),
                BootstrapKind.Parametric    => AvailableServices.GetRequiredService<ParametricBootstrapper>(),

                _ => throw new NotSupportedException($"Unknown bootstrapper kind: {GrowthOptions.Bootstrapper}"),
            };
        }
    }
}
