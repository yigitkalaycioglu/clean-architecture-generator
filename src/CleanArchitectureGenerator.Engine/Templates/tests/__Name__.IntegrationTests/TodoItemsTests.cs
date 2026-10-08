//#if Sample
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

namespace __Name__.IntegrationTests;

/// <summary>Örnek özelliğin uçtan uca davranışı ve kayıt sahipliği (IDOR) koruması.</summary>
public class TodoItemsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    [Fact]
    public async Task CreatedItem_CanBeReadAndListed()
    {
        using var client = await TestUsers.CreateAuthenticatedClientAsync(factory);

        var id = await CreateAsync(client, "Rapor yaz");
        var item = await client.GetFromJsonAsync<TodoItemBody>($"/api/todo-items/{id}", JsonOptions);
        var page = await client.GetFromJsonAsync<PageBody>("/api/todo-items?page=1&pageSize=10", JsonOptions);

        Assert.Equal("Rapor yaz", item!.Title);
        Assert.Equal(1, page!.TotalCount);
        Assert.Equal(id, Assert.Single(page.Items).Id);
    }

    [Fact]
    public async Task AnotherUsersItem_IsNotFound()
    {
        using var owner = await TestUsers.CreateAuthenticatedClientAsync(factory);
        using var stranger = await TestUsers.CreateAuthenticatedClientAsync(factory);
        var id = await CreateAsync(owner, "Gizli iş");

        using var read = await stranger.GetAsync($"/api/todo-items/{id}");
        using var delete = await stranger.DeleteAsync($"/api/todo-items/{id}");
        var strangersList = await stranger.GetFromJsonAsync<PageBody>("/api/todo-items", JsonOptions);

        // Başkasının kaydı "yok" sayılır; var olduğu bile anlaşılmaz.
        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        Assert.Empty(strangersList!.Items);
    }

    [Fact]
    public async Task UnknownField_IsRejected()
    {
        using var client = await TestUsers.CreateAuthenticatedClientAsync(factory);

        // Gövdeye sahiplik alanı eklenerek başka kullanıcı adına kayıt oluşturulamaz.
        using var response = await client.PostAsJsonAsync("/api/todo-items", new { title = "İş", priority = "Low", ownerId = "baska-kullanici" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MalformedJson_ReturnsProblemWithoutInternals()
    {
        using var client = await TestUsers.CreateAuthenticatedClientAsync(factory);
        using var content = new StringContent("{ \"title\": ", Encoding.UTF8, "application/json");

        using var response = await client.PostAsync("/api/todo-items", content);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.DoesNotContain("Exception", body, StringComparison.Ordinal);
        Assert.DoesNotContain(" at ", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InvalidTitle_ReturnsValidationProblem()
    {
        using var client = await TestUsers.CreateAuthenticatedClientAsync(factory);

        using var response = await client.PostAsJsonAsync("/api/todo-items", new { title = "", priority = "Low" });
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("title", problem!.Errors);
    }

    [Fact]
    public async Task CompletingTwice_IsAConflict()
    {
        using var client = await TestUsers.CreateAuthenticatedClientAsync(factory);
        var id = await CreateAsync(client, "Bir kez tamamlanır");

        using var first = await client.PostAsync($"/api/todo-items/{id}/complete", null);
        using var second = await client.PostAsync($"/api/todo-items/{id}/complete", null);

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    private static async Task<Guid> CreateAsync(HttpClient client, string title)
    {
        using var response = await client.PostAsJsonAsync("/api/todo-items", new { title, priority = "Medium" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CreatedBody>(JsonOptions);
        return created!.Id;
    }

    private sealed record CreatedBody(Guid Id);

    private sealed record TodoItemBody(Guid Id, string Title, string? Note, string Priority, bool IsDone);

    private sealed record PageBody(IReadOnlyList<TodoItemBody> Items, int Page, int PageSize, int TotalCount);
}
//#endif
