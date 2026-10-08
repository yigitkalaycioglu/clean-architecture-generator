//#if Sample
using __Name__.Application.TodoItems;
using __Name__.Domain.TodoItems;

namespace __Name__.UnitTests.TodoItems;

public class CreateTodoItemCommandValidatorTests
{
    private readonly CreateTodoItemCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_Passes()
    {
        var result = _validator.Validate(new CreateTodoItemCommand("Başlık", "Not", TodoItemPriority.Low));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyTitle_Fails()
    {
        var result = _validator.Validate(new CreateTodoItemCommand(string.Empty, null, TodoItemPriority.Low));

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateTodoItemCommand.Title));
    }

    [Fact]
    public void TooLongNote_Fails()
    {
        var result = _validator.Validate(new CreateTodoItemCommand("Başlık", new string('a', TodoItem.NoteMaxLength + 1), TodoItemPriority.Low));

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateTodoItemCommand.Note));
    }
}
//#endif
