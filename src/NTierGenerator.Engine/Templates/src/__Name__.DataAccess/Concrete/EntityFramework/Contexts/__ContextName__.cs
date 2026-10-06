using __Name__.Core.Entities;
//#if Auth
using __Name__.Core.Entities.Concrete;
//#endif
//#if Sample
using __Name__.Entities.Concrete;
//#endif
using Microsoft.EntityFrameworkCore;

namespace __Name__.DataAccess.Concrete.EntityFramework.Contexts;

/// <summary>
/// Uygulamanın EF Core veritabanı bağlamı. Tablo ayarları Configurations klasöründeki
/// IEntityTypeConfiguration sınıflarından otomatik yüklenir.
/// </summary>
public class __ContextName__(DbContextOptions<__ContextName__> options) : DbContext(options)
{
//#if Sample
    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Product> Products => Set<Product>();

//#endif
//#if Auth
    public DbSet<User> Users => Set<User>();

    public DbSet<OperationClaim> OperationClaims => Set<OperationClaim>();

    public DbSet<UserOperationClaim> UserOperationClaims => Set<UserOperationClaim>();

//#endif
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditInformation();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyAuditInformation();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(__ContextName__).Assembly);
    }

    /// <summary>Eklenen kayıtlara oluşturulma, güncellenen kayıtlara güncellenme tarihini yazar.</summary>
    private void ApplyAuditInformation()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedDate = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedDate = now;
                entry.Property(entity => entity.CreatedDate).IsModified = false;
            }
        }
    }
}
