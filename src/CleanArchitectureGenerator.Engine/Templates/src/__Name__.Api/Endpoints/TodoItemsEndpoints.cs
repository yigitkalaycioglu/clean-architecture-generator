//#if Sample
using __Name__.Api.Common;
using __Name__.Application.Abstractions.Messaging;
using __Name__.Application.Common;
using __Name__.Application.TodoItems;
using __Name__.Domain.TodoItems;

namespace __Name__.Api.Endpoints;

/// <summary>
/// Örnek özellik. Uç noktalar yalnızca isteği Application'a iletir ve sonucu HTTP yanıtına çevirir; kurallar
/// Domain'de, kullanım senaryoları Application'dadır. Her kullanıcı yalnızca kendi kayıtlarını görür.
/// </summary>
public static class TodoItemsEndpoints
{
    public static IEndpointRouteBuilder MapTodoItemsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/todo-items")
            .WithTags("TodoItems")
            .RequireAuthorization();

        group.MapGet("/", async (ISender sender, CancellationToken cancellationToken, int page = 1, int pageSize = Pagination.DefaultPageSize, bool? isDone = null) =>
        {
            var result = await sender.Send(new GetTodoItemsQuery(page, pageSize, isDone), cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
        })
        .Produces<PagedList<TodoItemResponse>>();

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetTodoItemQuery(id), cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
        })
        .Produces<TodoItemResponse>()
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", async (CreateTodoItemCommand command, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.IsSuccess
                ? Results.Created($"/api/todo-items/{result.Value}", new CreatedResponse(result.Value))
                : result.ToProblem();
        })
        .Produces<CreatedResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem();

        group.MapPut("/{id:guid}", async (Guid id, UpdateTodoItemRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new UpdateTodoItemCommand(id, request.Title, request.Note, request.Priority), cancellationToken);
            return result.IsSuccess ? Results.NoContent() : result.ToProblem();
        })
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/complete", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new CompleteTodoItemCommand(id), cancellationToken);
            return result.IsSuccess ? Results.NoContent() : result.ToProblem();
        })
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/reopen", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ReopenTodoItemCommand(id), cancellationToken);
            return result.IsSuccess ? Results.NoContent() : result.ToProblem();
        })
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new DeleteTodoItemCommand(id), cancellationToken);
            return result.IsSuccess ? Results.NoContent() : result.ToProblem();
        })
        .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}

/// <summary>Güncelleme gövdesi. Kimlik adresten gelir; sahiplik ve denetim alanları istemciden alınmaz.</summary>
public sealed record UpdateTodoItemRequest(string Title, string? Note, TodoItemPriority Priority);

public sealed record CreatedResponse(Guid Id);
//#endif
