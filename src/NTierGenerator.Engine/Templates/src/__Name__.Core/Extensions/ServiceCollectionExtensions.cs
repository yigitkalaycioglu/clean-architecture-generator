using __Name__.Core.Utilities.IoC;
using Microsoft.Extensions.DependencyInjection;

namespace __Name__.Core.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>Verilen Core modüllerinin servis kayıtlarını yükler.</summary>
    public static IServiceCollection AddDependencyResolvers(this IServiceCollection services, params ICoreModule[] modules)
    {
        foreach (var module in modules)
        {
            module.Load(services);
        }

        return services;
    }
}
