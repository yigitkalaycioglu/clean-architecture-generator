using __Name__.Application.Abstractions.Data;
using __Name__.Domain.Common;
//#if Sample
using __Name__.Domain.TodoItems;
//#endif
//#if LocalAuth
using __Name__.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
//#endif
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace __Name__.Infrastructure.Persistence;

//#if LocalAuth
public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options), IApplicationDbContext
//#else
public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IApplicationDbContext
//#endif
{
    /// <summary>Kaydı yapan kullanıcının kimliği en fazla bu uzunlukta olabilir ("sub" talebi).</summary>
    public const int UserIdMaxLength = 128;

//#if Sample
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

//#endif
//#if LocalAuth
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

//#endif
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Persistence/Configurations altındaki tüm IEntityTypeConfiguration sınıfları uygulanır.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes().Where(type => typeof(AuditableEntity).IsAssignableFrom(type.ClrType)))
        {
            modelBuilder.Entity(entityType.ClrType).Property(nameof(AuditableEntity.CreatedBy)).HasMaxLength(UserIdMaxLength);
            modelBuilder.Entity(entityType.ClrType).Property(nameof(AuditableEntity.LastModifiedBy)).HasMaxLength(UserIdMaxLength);
        }
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        // SQLite, DateTimeOffset değerlerini sıralayamaz ve karşılaştıramaz; bu sağlayıcıda sayı olarak saklanır.
        // (Entegrasyon testleri, seçilen veritabanından bağımsız olarak bellekte SQLite kullanır.)
        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
            configurationBuilder.Properties<DateTimeOffset?>().HaveConversion<DateTimeOffsetToBinaryConverter>();
        }
    }
}
