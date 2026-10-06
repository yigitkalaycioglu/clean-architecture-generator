using __Name__.Core.CrossCuttingConcerns.Caching;
using __Name__.Core.CrossCuttingConcerns.Caching.Microsoft;
using __Name__.Core.Utilities.IoC;
using Microsoft.Extensions.DependencyInjection;

namespace __Name__.Core.DependencyResolvers;

/// <summary>
/// Core katmanındaki çapraz kesen ilgilerin (cross-cutting concerns) servis kayıtları.
/// </summary>
public sealed class CoreModule : ICoreModule
{
    public void Load(IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddHttpContextAccessor();
        services.AddSingleton<ICacheManager, MemoryCacheManager>();
    }
}
