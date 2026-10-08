//#if Sample
using __Name__.Application.Abstractions.Authentication;
using __Name__.Application.Abstractions.Data;
using __Name__.Application.Abstractions.Messaging;
using __Name__.Domain.Common;
using __Name__.Domain.TodoItems;
using Microsoft.EntityFrameworkCore;

namespace __Name__.Application.TodoItems;

public sealed record ReopenTodoItemCommand(Guid Id) : ICommand;

internal sealed class ReopenTodoItemCommandHandler(IApplicationDbContext dbContext, IUserContext userContext)
    : ICommandHandler<ReopenTodoItemCommand>
{
    public async Task<Result> Handle(ReopenTodoItemCommand command, CancellationToken cancellationToken)
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

        var reopened = todoItem.Reopen();
        if (reopened.IsFailure)
        {
            return reopened;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
//#endif
