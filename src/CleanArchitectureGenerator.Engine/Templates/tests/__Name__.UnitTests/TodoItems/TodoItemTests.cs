//#if Sample
using __Name__.Domain.TodoItems;

namespace __Name__.UnitTests.TodoItems;

public class TodoItemTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_TrimsValues()
    {
        var result = TodoItem.Create("kullanici-1", "  Rapor yaz  ", "  ", TodoItemPriority.High);

        Assert.True(result.IsSuccess);
        Assert.Equal("Rapor yaz", result.Value.Title);
        Assert.Null(result.Value.Note);
        Assert.False(result.Value.IsDone);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutTitle_Fails(string title)
    {
        var result = TodoItem.Create("kullanici-1", title, null, TodoItemPriority.Low);

        Assert.Equal(TodoItemErrors.InvalidTitle, result.Error);
    }

    [Fact]
    public void Create_WithTooLongTitle_Fails()
    {
        var result = TodoItem.Create("kullanici-1", new string('a', TodoItem.TitleMaxLength + 1), null, TodoItemPriority.Low);

        Assert.Equal(TodoItemErrors.InvalidTitle, result.Error);
    }

    [Fact]
    public void Create_WithUndefinedPriority_Fails()
    {
        var result = TodoItem.Create("kullanici-1", "Başlık", null, (TodoItemPriority)42);

        Assert.Equal(TodoItemErrors.InvalidPriority, result.Error);
    }

    [Fact]
    public void Complete_MarksDoneAndRaisesEvent()
    {
        var todoItem = TodoItem.Create("kullanici-1", "Başlık", null, TodoItemPriority.Medium).Value;

        var result = todoItem.Complete(Now);

        Assert.True(result.IsSuccess);
        Assert.True(todoItem.IsDone);
        Assert.Equal(Now, todoItem.CompletedAt);
        var domainEvent = Assert.IsType<TodoItemCompletedEvent>(Assert.Single(todoItem.GetDomainEvents()));
        Assert.Equal(todoItem.Id, domainEvent.TodoItemId);
    }

    [Fact]
    public void Complete_Twice_Fails()
    {
        var todoItem = TodoItem.Create("kullanici-1", "Başlık", null, TodoItemPriority.Medium).Value;
        todoItem.Complete(Now);

        var result = todoItem.Complete(Now);

        Assert.Equal(TodoItemErrors.AlreadyCompleted, result.Error);
    }

    [Fact]
    public void Reopen_ClearsCompletion()
    {
        var todoItem = TodoItem.Create("kullanici-1", "Başlık", null, TodoItemPriority.Medium).Value;
        todoItem.Complete(Now);

        var result = todoItem.Reopen();

        Assert.True(result.IsSuccess);
        Assert.False(todoItem.IsDone);
        Assert.Null(todoItem.CompletedAt);
    }
}
//#endif
