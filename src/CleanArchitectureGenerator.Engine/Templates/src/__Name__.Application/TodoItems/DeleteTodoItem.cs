//#if Sample
using __Name__.Application.Abstractions.Authentication;
using __Name__.Application.Abstractions.Data;
using __Name__.Application.Abstractions.Messaging;
using __Name__.Domain.Common;
using __Name__.Domain.TodoItems;
using Microsoft.EntityFrameworkCore;

namespace __Name__.Application.TodoItems;

public sealed record DeleteTodoItemCommand(Guid Id) : ICommand;

internal sealed class DeleteTodoItemCommandHandler(IApplicationDbContext dbContext, IUserContext userContext)
    : ICommandHandler<DeleteTodoItemCommand>
{
    public async Task<Result> Handle(DeleteTodoItemCommand command, CancellationToken cancellationToken)
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

        dbContext.TodoItems.Remove(todoItem);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
//#endif
