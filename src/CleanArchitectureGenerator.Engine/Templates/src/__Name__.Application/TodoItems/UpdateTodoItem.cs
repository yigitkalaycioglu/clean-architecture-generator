//#if Sample
using __Name__.Application.Abstractions.Authentication;
using __Name__.Application.Abstractions.Data;
using __Name__.Application.Abstractions.Messaging;
using __Name__.Domain.Common;
using __Name__.Domain.TodoItems;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace __Name__.Application.TodoItems;

public sealed record UpdateTodoItemCommand(Guid Id, string Title, string? Note, TodoItemPriority Priority) : ICommand;

internal sealed class UpdateTodoItemCommandValidator : AbstractValidator<UpdateTodoItemCommand>
{
    public UpdateTodoItemCommandValidator()
    {
        RuleFor(command => command.Title).NotEmpty().MaximumLength(TodoItem.TitleMaxLength);
        RuleFor(command => command.Note).MaximumLength(TodoItem.NoteMaxLength);
        RuleFor(command => command.Priority).IsInEnum();
    }
}

internal sealed class UpdateTodoItemCommandHandler(IApplicationDbContext dbContext, IUserContext userContext)
    : ICommandHandler<UpdateTodoItemCommand>
{
    public async Task<Result> Handle(UpdateTodoItemCommand command, CancellationToken cancellationToken)
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

        var updated = todoItem.Update(command.Title, command.Note, command.Priority);
        if (updated.IsFailure)
        {
            return updated;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
//#endif
