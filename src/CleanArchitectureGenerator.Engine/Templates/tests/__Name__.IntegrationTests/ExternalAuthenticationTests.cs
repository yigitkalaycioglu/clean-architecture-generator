//#if ExternalAuth
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace __Name__.IntegrationTests;

/// <summary>Harici sağlayıcının token'ları: yalnızca doğru yayıncı, hedef kitle ve asimetrik imza kabul edilir.</summary>
public class ExternalAuthenticationTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task ValidToken_IsAccepted()
    {
        using var response = await GetAccountAsync(TestTokens.Create("kullanici-1"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TokenForAnotherApi_IsRejected()
    {
        using var response = await GetAccountAsync(TestTokens.Create("kullanici-1", audience: "api://baska-bir-api"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ExpiredToken_IsRejected()
    {
        using var response = await GetAccountAsync(TestTokens.Create("kullanici-1", expiresAt: DateTimeOffset.UtcNow.AddMinutes(-5)));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SymmetricallySignedToken_IsRejected()
    {
        // Algoritma karıştırma saldırısı: HMAC ile imzalanmış token, anahtarı bilinse bile kabul edilmez.
        var key = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(64));
        var token = TestTokens.Create("kullanici-1", signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        using var response = await GetAccountAsync(token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<HttpResponseMessage> GetAccountAsync(string token)
    {
        using var client = factory.CreateHttpsClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.GetAsync("/api/account");
    }
}
//#endif
