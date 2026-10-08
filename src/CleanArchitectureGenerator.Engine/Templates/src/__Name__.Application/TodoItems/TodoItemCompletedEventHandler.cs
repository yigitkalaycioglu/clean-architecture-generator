//#if Sample
using __Name__.Application.Abstractions.Messaging;
using __Name__.Domain.TodoItems;
using Microsoft.Extensions.Logging;

namespace __Name__.Application.TodoItems;

/// <summary>
/// Örnek alan olayı işleyicisi. Gerçek bir projede burada bildirim gönderilebilir, istatistik güncellenebilir
/// ya da başka bir modüle haber verilebilir; aynı olaya birden fazla işleyici eklenebilir.
/// </summary>
internal sealed class TodoItemCompletedEventHandler(ILogger<TodoItemCompletedEventHandler> logger)
    : IDomainEventHandler<TodoItemCompletedEvent>
{
    public Task Handle(TodoItemCompletedEvent domainEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        logger.LogInformation("Yapılacak iş tamamlandı: {TodoItemId}", domainEvent.TodoItemId);
        return Task.CompletedTask;
    }
}
//#endif
