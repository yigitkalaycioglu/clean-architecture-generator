using Microsoft.Extensions.DependencyInjection;

namespace __Name__.Core.Utilities.IoC;

/// <summary>
/// Aspect'ler attribute olduğu için constructor injection alamaz; servislere buradan erişirler.
/// Uygulama başlarken <see cref="Initialize"/> ile kök servis sağlayıcı verilir.
/// Yalnızca singleton servisler (cache, HTTP bağlamı, logger) için kullanın.
/// </summary>
public static class ServiceTool
{
    private static IServiceProvider? _serviceProvider;

    public static IServiceProvider ServiceProvider =>
        _serviceProvider ?? throw new InvalidOperationException(
            "ServiceTool başlatılmamış. Program.cs içinde 'ServiceTool.Initialize(app.Services)' çağrısı olmalı.");

    public static void Initialize(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        _serviceProvider = serviceProvider;
    }

    public static T GetRequiredService<T>()
        where T : notnull
    {
        return ServiceProvider.GetRequiredService<T>();
    }
}
