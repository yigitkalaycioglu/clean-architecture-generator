using __Name__.Domain.Common;

namespace __Name__.Application.Abstractions.Messaging;

/// <summary>
/// Bir alan olayına tepki verir. İşleyiciler değişikliklerle aynı kaydetme işlemi içinde, veritabanına
/// yazmadan hemen önce çalışır; burada yapılan değişiklikler de aynı işlemde kaydedilir.
/// </summary>
public interface IDomainEventHandler<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    Task Handle(TDomainEvent domainEvent, CancellationToken cancellationToken);
}

/// <summary>Alan olaylarını işleyicilerine dağıtır. Infrastructure'daki kaydetme kesicisi tarafından çağrılır.</summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
