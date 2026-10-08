//#if Sample
using __Name__.Domain.Common;

namespace __Name__.Domain.TodoItems;

public static class TodoItemErrors
{
    public static readonly Error NotFound = Error.NotFound("TodoItem.NotFound", "Yapılacak iş bulunamadı.");

    public static readonly Error InvalidOwner = Error.Validation("TodoItem.InvalidOwner", "İşin sahibi geçersiz.");

    public static readonly Error InvalidTitle = Error.Validation(
        "TodoItem.InvalidTitle", $"Başlık boş olamaz ve en fazla {TodoItem.TitleMaxLength} karakter olabilir.");

    public static readonly Error InvalidNote = Error.Validation(
        "TodoItem.InvalidNote", $"Not en fazla {TodoItem.NoteMaxLength} karakter olabilir.");

    public static readonly Error InvalidPriority = Error.Validation("TodoItem.InvalidPriority", "Öncelik geçersiz.");

    public static readonly Error AlreadyCompleted = Error.Conflict("TodoItem.AlreadyCompleted", "Bu iş zaten tamamlanmış.");

    public static readonly Error NotCompleted = Error.Conflict("TodoItem.NotCompleted", "Bu iş henüz tamamlanmamış.");
}
//#endif
