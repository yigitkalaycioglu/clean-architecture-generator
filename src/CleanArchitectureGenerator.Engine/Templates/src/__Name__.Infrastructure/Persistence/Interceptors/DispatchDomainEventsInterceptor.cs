using __Name__.Application.Abstractions.Messaging;
using __Name__.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace __Name__.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Kaydetmeden hemen önce varlıkların biriktirdiği alan olaylarını işleyicilerine iletir. İşleyicilerin yaptığı
/// değişiklikler de aynı SaveChanges çağrısıyla, aynı işlem içinde kaydedilir.
/// </summary>
internal sealed class DispatchDomainEventsInterceptor(IDomainEventDispatcher dispatcher) : SaveChangesInterceptor
{
    /// <summary>İşleyiciler yeni olaylar doğurabilir; birbirini sonsuza dek tetikleyen işleyicilere karşı sınır.</summary>
    private const int MaxDispatchRounds = 10;

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        if (eventData.Context is not null && CollectEntitiesWithEvents(eventData.Context).Count > 0)
        {
            throw new InvalidOperationException("Alan olayı bekleyen varlıklar SaveChangesAsync ile kaydedilmelidir.");
        }

        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        if (eventData.Context is not null)
        {
            await DispatchAsync(eventData.Context, cancellationToken);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private async Task DispatchAsync(DbContext context, CancellationToken cancellationToken)
    {
        for (var round = 0; round < MaxDispatchRounds; round++)
        {
            var entities = CollectEntitiesWithEvents(context);
            if (entities.Count == 0)
            {
                return;
            }

            var domainEvents = entities.SelectMany(entity => entity.GetDomainEvents()).ToList();
            entities.ForEach(entity => entity.ClearDomainEvents());
            await dispatcher.DispatchAsync(domainEvents, cancellationToken);
        }

        throw new InvalidOperationException($"Alan olayları {MaxDispatchRounds} turda tükenmedi; işleyiciler birbirini döngüye sokuyor olabilir.");
    }

    private static List<Entity> CollectEntitiesWithEvents(DbContext context) =>
        context.ChangeTracker.Entries<Entity>()
            .Select(entry => entry.Entity)
            .Where(entity => entity.GetDomainEvents().Count > 0)
            .ToList();
}
