//#if Sample
using __Name__.Domain.Common;

namespace __Name__.Domain.TodoItems;

/// <summary>
/// Örnek varlık: bir kullanıcıya ait yapılacak iş. Durumu yalnızca metotlarla değişir; böylece kurallar
/// (başlık zorunlu, tamamlanmış iş yeniden tamamlanamaz…) tek yerde korunur ve atlanamaz.
/// </summary>
public sealed class TodoItem : AuditableEntity
{
    public const int OwnerIdMaxLength = 128;
    public const int TitleMaxLength = 200;
    public const int NoteMaxLength = 2000;

    // EF Core, kayıtları veritabanından okurken bu yapıcıyı kullanır.
    private TodoItem()
    {
    }

    /// <summary>İşin sahibi olan kullanıcının kimliği (token'daki "sub" değeri).</summary>
    public string OwnerId { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public string? Note { get; private set; }

    public TodoItemPriority Priority { get; private set; }

    public bool IsDone { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public static Result<TodoItem> Create(string ownerId, string title, string? note, TodoItemPriority priority)
    {
        if (string.IsNullOrWhiteSpace(ownerId) || ownerId.Length > OwnerIdMaxLength)
        {
            return TodoItemErrors.InvalidOwner;
        }

        var todoItem = new TodoItem { OwnerId = ownerId };
        var updated = todoItem.Update(title, note, priority);
        if (updated.IsFailure)
        {
            return updated.Error;
        }

        return todoItem;
    }

    public Result Update(string title, string? note, TodoItemPriority priority)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > TitleMaxLength)
        {
            return TodoItemErrors.InvalidTitle;
        }

        if (note?.Trim().Length > NoteMaxLength)
        {
            return TodoItemErrors.InvalidNote;
        }

        if (!Enum.IsDefined(priority))
        {
            return TodoItemErrors.InvalidPriority;
        }

        Title = title.Trim();
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        Priority = priority;
        return Result.Success();
    }

    public Result Complete(DateTimeOffset completedAt)
    {
        if (IsDone)
        {
            return TodoItemErrors.AlreadyCompleted;
        }

        IsDone = true;
        CompletedAt = completedAt;
        Raise(new TodoItemCompletedEvent(Id, OwnerId));
        return Result.Success();
    }

    public Result Reopen()
    {
        if (!IsDone)
        {
            return TodoItemErrors.NotCompleted;
        }

        IsDone = false;
        CompletedAt = null;
        return Result.Success();
    }
}
//#endif
