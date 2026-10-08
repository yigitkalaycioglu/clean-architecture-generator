//#if Sample
using __Name__.Domain.Common;

namespace __Name__.Domain.TodoItems;

public sealed record TodoItemCompletedEvent(Guid TodoItemId, string OwnerId) : IDomainEvent;
//#endif
