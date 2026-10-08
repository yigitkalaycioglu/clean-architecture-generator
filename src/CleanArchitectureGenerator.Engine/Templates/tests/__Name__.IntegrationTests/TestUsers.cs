using System.Net.Http.Headers;
//#if LocalAuth
using System.Net.Http.Json;
//#endif

namespace __Name__.IntegrationTests;

/// <summary>Testlerde oturum açmış kullanıcı oluşturur.</summary>
public static class TestUsers
{
//#if LocalAuth
    public const string Password = "Dogru-At-Pil-Zimba-42";

    public static string NewEmail() => $"kullanici-{Guid.NewGuid():N}@test.local";

    /// <summary>Kayıt olur, e-postadaki bağlantıyla hesabı doğrular ve giriş yapar; erişim token'ını döndürür.</summary>
    public static async Task<string> RegisterAndLoginAsync(ApiFactory factory, HttpClient client, string email)
    {
        (await client.PostAsJsonAsync("/api/auth/register", new { email, password = Password })).EnsureSuccessStatusCode();

        var link = factory.Emails.LastLinkParameters(email);
        (await client.PostAsJsonAsync("/api/auth/confirm-email", new { userId = link["userId"], code = link["code"] })).EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        login.EnsureSuccessStatusCode();
        var tokens = await login.Content.ReadFromJsonAsync<AccessTokenBody>();
        return tokens!.AccessToken;
    }

    /// <summary>Yeni bir kullanıcıyla oturum açmış istemci.</summary>
    public static async Task<HttpClient> CreateAuthenticatedClientAsync(ApiFactory factory)
    {
        var client = factory.CreateHttpsClient();
        var accessToken = await RegisterAndLoginAsync(factory, client, NewEmail());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    public sealed record AccessTokenBody(string AccessToken, DateTimeOffset ExpiresAt, string TokenType);
//#else
    /// <summary>Kimlik sağlayıcıdan alınmış gibi imzalanan token'la, yeni bir kullanıcı adına istemci.</summary>
    public static Task<HttpClient> CreateAuthenticatedClientAsync(ApiFactory factory)
    {
        var client = factory.CreateHttpsClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestTokens.Create($"kullanici-{Guid.NewGuid():N}"));
        return Task.FromResult(client);
    }
//#endif
}
