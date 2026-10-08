using System.Collections.Concurrent;
using __Name__.Application.Abstractions.Messaging;
using __Name__.Domain.Common;
using Microsoft.Extensions.DependencyInjection;

namespace __Name__.Application.Messaging;

internal sealed class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    private static readonly ConcurrentDictionary<Type, DomainEventPublisher> Publishers = new();

    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);

        foreach (var domainEvent in domainEvents)
        {
            var publisher = Publishers.GetOrAdd(
                domainEvent.GetType(),
                static eventType => (DomainEventPublisher)Activator.CreateInstance(typeof(DomainEventPublisher<>).MakeGenericType(eventType))!);

            await publisher.Publish(domainEvent, serviceProvider, cancellationToken);
        }
    }

    private abstract class DomainEventPublisher
    {
        public abstract Task Publish(IDomainEvent domainEvent, IServiceProvider serviceProvider, CancellationToken cancellationToken);
    }

    private sealed class DomainEventPublisher<TDomainEvent> : DomainEventPublisher
        where TDomainEvent : IDomainEvent
    {
        public override async Task Publish(IDomainEvent domainEvent, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            foreach (var handler in serviceProvider.GetServices<IDomainEventHandler<TDomainEvent>>())
            {
                await handler.Handle((TDomainEvent)domainEvent, cancellationToken);
            }
        }
    }
}
