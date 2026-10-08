//#if Sample
using __Name__.Application.Abstractions.Authentication;
using __Name__.Application.Abstractions.Data;
using __Name__.Application.Abstractions.Messaging;
using __Name__.Application.Common;
using __Name__.Domain.Common;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace __Name__.Application.TodoItems;

public sealed record GetTodoItemsQuery(int Page, int PageSize, bool? IsDone) : IQuery<PagedList<TodoItemResponse>>;

internal sealed class GetTodoItemsQueryValidator : AbstractValidator<GetTodoItemsQuery>
{
    public GetTodoItemsQueryValidator()
    {
        RuleFor(query => query.Page).InclusiveBetween(1, Pagination.MaxPage);
        RuleFor(query => query.PageSize).InclusiveBetween(1, Pagination.MaxPageSize);
    }
}

internal sealed class GetTodoItemsQueryHandler(IApplicationDbContext dbContext, IUserContext userContext)
    : IQueryHandler<GetTodoItemsQuery, PagedList<TodoItemResponse>>
{
    public async Task<Result<PagedList<TodoItemResponse>>> Handle(GetTodoItemsQuery query, CancellationToken cancellationToken)
    {
        if (userContext.UserId is not { } userId)
        {
            return UserContextErrors.NotAuthenticated;
        }

        // Kullanıcı yalnızca kendi kayıtlarını görür; filtre sorgunun kendisindedir.
        var todoItems = dbContext.TodoItems.AsNoTracking().Where(todoItem => todoItem.OwnerId == userId);
        if (query.IsDone is { } isDone)
        {
            todoItems = todoItems.Where(todoItem => todoItem.IsDone == isDone);
        }

        return await todoItems
            .OrderByDescending(todoItem => todoItem.CreatedAt)
            .ThenBy(todoItem => todoItem.Id)
            .Select(TodoItemResponse.Projection)
            .ToPagedListAsync(query.Page, query.PageSize, cancellationToken);
    }
}
//#endif
