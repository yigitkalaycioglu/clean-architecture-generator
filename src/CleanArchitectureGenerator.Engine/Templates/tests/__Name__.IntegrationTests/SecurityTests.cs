using System.Net;
using System.Net.Http.Headers;

namespace __Name__.IntegrationTests;

/// <summary>Tüm uygulama için geçerli güvenlik davranışları: başlıklar, varsayılan olarak kapalı uç noktalar, hata yanıtları.</summary>
public class SecurityTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Responses_CarrySecurityHeaders()
    {
        using var client = factory.CreateHttpsClient();

        using var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("nosniff", Header(response, "X-Content-Type-Options"));
        Assert.Equal("DENY", Header(response, "X-Frame-Options"));
        Assert.Equal("no-referrer", Header(response, "Referrer-Policy"));
        Assert.StartsWith("default-src 'none'", Header(response, "Content-Security-Policy"), StringComparison.Ordinal);
        Assert.Contains("no-store", Header(response, "Cache-Control"), StringComparison.Ordinal);
        Assert.False(response.Headers.Contains("Server"));
    }

    [Theory]
    [InlineData("/api/account")]
    [InlineData("/api/bilinmeyen-adres")]
    public async Task Endpoints_RequireAuthenticationByDefault(string path)
    {
        using var client = factory.CreateHttpsClient();

        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TamperedToken_IsRejected()
    {
        using var client = await TestUsers.CreateAuthenticatedClientAsync(factory);
        var token = client.DefaultRequestHeaders.Authorization!.Parameter!;

        // İmzanın bir karakteri değiştirilir: içerik aynı kalsa da token geçersizdir.
        var index = token.Length - 5;
        var tampered = string.Concat(token.AsSpan(0, index), token[index] == 'A' ? "B" : "A", token.AsSpan(index + 1));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tampered);

        using var response = await client.GetAsync("/api/account");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedUser_ReachesProtectedEndpoint()
    {
        using var client = await TestUsers.CreateAuthenticatedClientAsync(factory);

        using var response = await client.GetAsync("/api/account");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static string? Header(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out var values) || response.Content.Headers.TryGetValues(name, out values)
            ? string.Join(", ", values)
            : null;
}
