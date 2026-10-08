//#if Sample
using __Name__.Application.Abstractions.Authentication;
using __Name__.Application.Abstractions.Data;
using __Name__.Application.Abstractions.Messaging;
using __Name__.Domain.Common;
using __Name__.Domain.TodoItems;
using Microsoft.EntityFrameworkCore;

namespace __Name__.Application.TodoItems;

public sealed record GetTodoItemQuery(Guid Id) : IQuery<TodoItemResponse>;

internal sealed class GetTodoItemQueryHandler(IApplicationDbContext dbContext, IUserContext userContext)
    : IQueryHandler<GetTodoItemQuery, TodoItemResponse>
{
    public async Task<Result<TodoItemResponse>> Handle(GetTodoItemQuery query, CancellationToken cancellationToken)
    {
        if (userContext.UserId is not { } userId)
        {
            return UserContextErrors.NotAuthenticated;
        }

        // Başkasına ait kayıt "yok" sayılır (404): kaydın var olduğu bile sızdırılmaz (IDOR koruması).
        var todoItem = await dbContext.TodoItems
            .AsNoTracking()
            .Where(todoItem => todoItem.Id == query.Id && todoItem.OwnerId == userId)
            .Select(TodoItemResponse.Projection)
            .FirstOrDefaultAsync(cancellationToken);

        return todoItem is null ? TodoItemErrors.NotFound : todoItem;
    }
}
//#endif
