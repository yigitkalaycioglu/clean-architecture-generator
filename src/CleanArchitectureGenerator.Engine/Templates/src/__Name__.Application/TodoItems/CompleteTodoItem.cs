//#if Sample
using __Name__.Application.Abstractions.Authentication;
using __Name__.Application.Abstractions.Data;
using __Name__.Application.Abstractions.Messaging;
using __Name__.Domain.Common;
using __Name__.Domain.TodoItems;
using Microsoft.EntityFrameworkCore;

namespace __Name__.Application.TodoItems;

public sealed record CompleteTodoItemCommand(Guid Id) : ICommand;

internal sealed class CompleteTodoItemCommandHandler(IApplicationDbContext dbContext, IUserContext userContext, TimeProvider timeProvider)
    : ICommandHandler<CompleteTodoItemCommand>
{
    public async Task<Result> Handle(CompleteTodoItemCommand command, CancellationToken cancellationToken)
    {
        if (userContext.UserId is not { } userId)
        {
            return UserContextErrors.NotAuthenticated;
        }

        var todoItem = await dbContext.TodoItems
            .FirstOrDefaultAsync(todoItem => todoItem.Id == command.Id && todoItem.OwnerId == userId, cancellationToken);
        if (todoItem is null)
        {
            return TodoItemErrors.NotFound;
        }

        // Varlık TodoItemCompletedEvent olayını kaydeder; olay SaveChanges sırasında işleyicilerine iletilir.
        var completed = todoItem.Complete(timeProvider.GetUtcNow());
        if (completed.IsFailure)
        {
            return completed;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
//#endif
