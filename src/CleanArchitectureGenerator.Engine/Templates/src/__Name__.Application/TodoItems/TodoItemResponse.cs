//#if Sample
using System.Linq.Expressions;
using __Name__.Domain.TodoItems;

namespace __Name__.Application.TodoItems;

/// <summary>
/// API'nin döndürdüğü görünüm. Varlığın kendisi dışarı verilmez: hangi alanların görüneceği burada açıkça seçilir
/// (OwnerId gibi iç alanlar sızmaz).
/// </summary>
public sealed record TodoItemResponse(
    Guid Id,
    string Title,
    string? Note,
    TodoItemPriority Priority,
    bool IsDone,
    DateTimeOffset? CompletedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastModifiedAt)
{
    /// <summary>Sorgularda SQL'e çevrilen eşleme: yalnızca gereken sütunlar okunur.</summary>
    internal static Expression<Func<TodoItem, TodoItemResponse>> Projection { get; } = todoItem => new TodoItemResponse(
        todoItem.Id,
        todoItem.Title,
        todoItem.Note,
        todoItem.Priority,
        todoItem.IsDone,
        todoItem.CompletedAt,
        todoItem.CreatedAt,
        todoItem.LastModifiedAt);
}
//#endif
