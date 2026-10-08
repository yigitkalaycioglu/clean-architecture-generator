//#if Sample
using __Name__.Application.Abstractions.Authentication;
using __Name__.Application.Abstractions.Data;
using __Name__.Application.Abstractions.Messaging;
using __Name__.Domain.Common;
using __Name__.Domain.TodoItems;
using FluentValidation;

namespace __Name__.Application.TodoItems;

public sealed record CreateTodoItemCommand(string Title, string? Note, TodoItemPriority Priority) : ICommand<Guid>;

internal sealed class CreateTodoItemCommandValidator : AbstractValidator<CreateTodoItemCommand>
{
    public CreateTodoItemCommandValidator()
    {
        RuleFor(command => command.Title).NotEmpty().MaximumLength(TodoItem.TitleMaxLength);
        RuleFor(command => command.Note).MaximumLength(TodoItem.NoteMaxLength);
        RuleFor(command => command.Priority).IsInEnum();
    }
}

internal sealed class CreateTodoItemCommandHandler(IApplicationDbContext dbContext, IUserContext userContext)
    : ICommandHandler<CreateTodoItemCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateTodoItemCommand command, CancellationToken cancellationToken)
    {
        if (userContext.UserId is not { } userId)
        {
            return UserContextErrors.NotAuthenticated;
        }

        // Sahip her zaman token'dan gelir; istemci başka bir kullanıcı adına kayıt oluşturamaz.
        var created = TodoItem.Create(userId, command.Title, command.Note, command.Priority);
        if (created.IsFailure)
        {
            return created.Error;
        }

        dbContext.TodoItems.Add(created.Value);
        await dbContext.SaveChangesAsync(cancellationToken);
        return created.Value.Id;
    }
}
//#endif
