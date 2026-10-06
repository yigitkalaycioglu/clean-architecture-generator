using __Name__.DataAccess.Concrete.EntityFramework.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace __Name__.DataAccess;

public static class DataAccessServiceRegistration
{
    public static IServiceCollection AddDataAccessServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("'ConnectionStrings:DefaultConnection' ayarı bulunamadı (appsettings.json).");

        services.AddDbContext<__ContextName__>(options => options.__DbUseMethod__(connectionString));

        // Konvansiyon: adı "Dal" ile biten her sınıf, adı "Dal" ile biten arayüzüyle Scoped kaydedilir
        // (EfProductDal -> IProductDal). Yeni bir Dal eklediğinizde ayrıca kayıt yapmanız gerekmez.
        var dalTypes = typeof(DataAccessServiceRegistration).Assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && type.Name.EndsWith("Dal", StringComparison.Ordinal));

        foreach (var implementationType in dalTypes)
        {
            foreach (var serviceType in implementationType.GetInterfaces().Where(type => type.Name.EndsWith("Dal", StringComparison.Ordinal)))
            {
                services.AddScoped(serviceType, implementationType);
            }
        }

        return services;
    }

    /// <summary>
    /// Bekleyen EF Core migration'larını veritabanına uygular; veritabanı yoksa oluşturur.
    /// Geliştirme ortamında uygulama açılırken çağrılır.
    /// </summary>
    public static async Task ApplyDatabaseMigrationsAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<__ContextName__>();

        var pendingMigrations = await context.Database.GetPendingMigrationsAsync(cancellationToken);
        if (pendingMigrations.Any())
        {
            await context.Database.MigrateAsync(cancellationToken);
        }
    }
}
