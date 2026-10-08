using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace __Name__.Infrastructure.Persistence;

public static class DatabaseMigrator
{
    /// <summary>
    /// Bekleyen EF Core migration'larını uygular; veritabanı yoksa oluşturur. Yalnızca geliştirme ortamında
    /// çağrılır: canlıda şema değişiklikleri uygulama açılırken değil, dağıtım adımında (migration bundle ya da
    /// SQL betiği) ve uygulamanın kullanıcısından daha yetkili bir hesapla uygulanmalıdır.
    /// </summary>
    public static async Task ApplyMigrationsAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        // GetMigrations veritabanına bağlanmaz; ilk migration eklenene kadar yapılacak bir şey yoktur.
        if (!context.Database.GetMigrations().Any())
        {
            logger.LogWarning("Henüz migration yok, veritabanı oluşturulmadı. README'deki \"dotnet ef migrations add InitialCreate\" adımını uygulayın.");
            return;
        }

        try
        {
            await context.Database.MigrateAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Migration'lar uygulanamadı. Bağlantı cümlesini ve veritabanı sunucusunun çalıştığını kontrol edin.");
        }
    }
}
