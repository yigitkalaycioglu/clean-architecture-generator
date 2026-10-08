using __Name__.Application.Abstractions.Authentication;
using __Name__.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace __Name__.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Kaydetmeden hemen önce oluşturma ve değişiklik bilgilerini (zaman ve kullanıcı) doldurur. Bu alanlar
/// istemciden gelen veriyle değiştirilemez; yalnızca burada, sunucu saatiyle yazılır.
/// </summary>
internal sealed class AuditableEntityInterceptor(IUserContext userContext, TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        UpdateAuditFields(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        UpdateAuditFields(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateAuditFields(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = timeProvider.GetUtcNow();
        var userId = userContext.UserId;
        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(entity => entity.CreatedAt).CurrentValue = now;
                entry.Property(entity => entity.CreatedBy).CurrentValue = userId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(entity => entity.LastModifiedAt).CurrentValue = now;
                entry.Property(entity => entity.LastModifiedBy).CurrentValue = userId;
            }
        }
    }
}
