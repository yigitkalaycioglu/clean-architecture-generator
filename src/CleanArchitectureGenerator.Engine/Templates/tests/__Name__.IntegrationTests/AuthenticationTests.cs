//#if LocalAuth
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using __Name__.Api.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace __Name__.IntegrationTests;

/// <summary>Kayıt, giriş ve oturum akışlarının güvenlik davranışları.</summary>
public class AuthenticationTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task WrongPassword_AndUnknownEmail_GetTheSameAnswer()
    {
        using var client = factory.CreateHttpsClient();
        var email = TestUsers.NewEmail();
        await TestUsers.RegisterAndLoginAsync(factory, client, email);

        using var wrongPassword = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "baska-bir-parola-123" });
        using var unknownEmail = await client.PostAsJsonAsync("/api/auth/login", new { email = TestUsers.NewEmail(), password = "baska-bir-parola-123" });

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);
        Assert.Equal(await ErrorCodeAsync(wrongPassword), await ErrorCodeAsync(unknownEmail));
    }

    [Fact]
    public async Task RegisteringAnExistingEmail_LooksLikeANewRegistration()
    {
        using var client = factory.CreateHttpsClient();
        var email = TestUsers.NewEmail();

        using var first = await client.PostAsJsonAsync("/api/auth/register", new { email, password = TestUsers.Password });
        using var second = await client.PostAsJsonAsync("/api/auth/register", new { email, password = TestUsers.Password });

        Assert.Equal(HttpStatusCode.Accepted, first.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, second.StatusCode);
        Assert.Equal(2, factory.Emails.MessagesTo(email).Count);
    }

    [Fact]
    public async Task CommonPassword_IsRejected()
    {
        using var client = factory.CreateHttpsClient();

        using var response = await client.PostAsJsonAsync("/api/auth/register", new { email = TestUsers.NewEmail(), password = "password1234" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        Assert.Contains("password", problem!.Errors);
    }

    [Fact]
    public async Task UnconfirmedEmail_CannotLogIn()
    {
        using var client = factory.CreateHttpsClient();
        var email = TestUsers.NewEmail();
        (await client.PostAsJsonAsync("/api/auth/register", new { email, password = TestUsers.Password })).EnsureSuccessStatusCode();

        using var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = TestUsers.Password });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RepeatedWrongPasswords_LockTheAccount()
    {
        using var client = factory.CreateHttpsClient();
        var email = TestUsers.NewEmail();
        await TestUsers.RegisterAndLoginAsync(factory, client, email);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var failed = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "baska-bir-parola-123" });
            Assert.Equal(HttpStatusCode.Unauthorized, failed.StatusCode);
        }

        // Hesap kilitliyken doğru parola da kabul edilmez.
        using var locked = await client.PostAsJsonAsync("/api/auth/login", new { email, password = TestUsers.Password });
        Assert.Equal(HttpStatusCode.Unauthorized, locked.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_IsRotated_AndReuseEndsTheSession()
    {
        using var client = factory.CreateHttpsClient();
        var email = TestUsers.NewEmail();
        await TestUsers.RegisterAndLoginAsync(factory, client, email);
        using var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = TestUsers.Password });
        var originalRefreshToken = RefreshTokenFrom(login);

        using var refreshed = await client.PostAsync("/api/auth/refresh", null);
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        Assert.NotEqual(originalRefreshToken, RefreshTokenFrom(refreshed));

        // Kullanılmış token tekrar gönderilir (çalınmış bir kopya gibi): reddedilir ve oturum ailesi kapanır.
        using var attacker = factory.CreateHttpsClient(handleCookies: false);
        using var replayRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        replayRequest.Headers.Add("Cookie", $"{AuthEndpoints.RefreshTokenCookie}={originalRefreshToken}");
        using var replay = await attacker.SendAsync(replayRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);

        // Meşru istemcinin elindeki yeni token da artık geçersizdir; yeniden giriş gerekir.
        using var afterReuse = await client.PostAsync("/api/auth/refresh", null);
        Assert.Equal(HttpStatusCode.Unauthorized, afterReuse.StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesTheRefreshToken()
    {
        using var client = factory.CreateHttpsClient();
        await TestUsers.RegisterAndLoginAsync(factory, client, TestUsers.NewEmail());

        using var logout = await client.PostAsync("/api/auth/logout", null);
        using var refresh = await client.PostAsync("/api/auth/refresh", null);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task RefreshTokenCookie_IsHttpOnlySecureAndStrict()
    {
        using var client = factory.CreateHttpsClient();
        var email = TestUsers.NewEmail();
        await TestUsers.RegisterAndLoginAsync(factory, client, email);

        using var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = TestUsers.Password });
        var cookie = login.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith(AuthEndpoints.RefreshTokenCookie, StringComparison.Ordinal));

        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refreshToken", await login.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TwoFactor_IsRequiredOnceEnabled()
    {
        using var client = factory.CreateHttpsClient();
        var email = TestUsers.NewEmail();
        var accessToken = await TestUsers.RegisterAndLoginAsync(factory, client, email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var setup = await client.PostAsync("/api/account/2fa/setup", null);
        var sharedKey = (await setup.Content.ReadFromJsonAsync<TwoFactorSetupBody>())!.SharedKey;
        using var enable = await client.PostAsJsonAsync("/api/account/2fa/enable", new { code = Totp.Compute(sharedKey) });
        var recoveryCodes = (await enable.Content.ReadFromJsonAsync<RecoveryCodesBody>())!.RecoveryCodes;
        Assert.Equal(10, recoveryCodes.Count);

        using var withoutCode = await client.PostAsJsonAsync("/api/auth/login", new { email, password = TestUsers.Password });
        Assert.Equal(HttpStatusCode.Unauthorized, withoutCode.StatusCode);
        Assert.Equal("Auth.TwoFactorRequired", await ErrorCodeAsync(withoutCode));

        using var withCode = await client.PostAsJsonAsync("/api/auth/login", new { email, password = TestUsers.Password, twoFactorCode = Totp.Compute(sharedKey) });
        Assert.Equal(HttpStatusCode.OK, withCode.StatusCode);

        // Kurtarma kodu tek kullanımlıktır.
        using var withRecoveryCode = await client.PostAsJsonAsync("/api/auth/login", new { email, password = TestUsers.Password, recoveryCode = recoveryCodes[0] });
        using var reusedRecoveryCode = await client.PostAsJsonAsync("/api/auth/login", new { email, password = TestUsers.Password, recoveryCode = recoveryCodes[0] });
        Assert.Equal(HttpStatusCode.OK, withRecoveryCode.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, reusedRecoveryCode.StatusCode);
    }

    private static string RefreshTokenFrom(HttpResponseMessage response)
    {
        var cookie = response.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith(AuthEndpoints.RefreshTokenCookie + "=", StringComparison.Ordinal));
        return cookie[(AuthEndpoints.RefreshTokenCookie.Length + 1)..cookie.IndexOf(';', StringComparison.Ordinal)];
    }

    private static async Task<string?> ErrorCodeAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        return problem?.Extensions["code"]?.ToString();
    }

    private sealed record TwoFactorSetupBody(string SharedKey, string AuthenticatorUri);

    private sealed record RecoveryCodesBody(IReadOnlyList<string> RecoveryCodes);
}
//#endif
