using System.Net;
//#if LocalAuth
using System.Net.Http.Json;
//#endif

namespace __Name__.IntegrationTests;

public sealed class GlobalRateLimitApiFactory : ApiFactory
{
    protected override Dictionary<string, string?> CreateSettings()
    {
        var settings = base.CreateSettings();
        settings["RateLimiting:PermitLimit"] = "3";
        return settings;
    }
}

/// <summary>Aynı istemciden gelen istekler sınırı aşınca 429 ve Retry-After döner.</summary>
public class GlobalRateLimitTests(GlobalRateLimitApiFactory factory) : IClassFixture<GlobalRateLimitApiFactory>
{
    [Fact]
    public async Task TooManyRequests_AreRejected()
    {
        using var client = factory.CreateHttpsClient();
        for (var attempt = 0; attempt < 3; attempt++)
        {
            using var allowed = await client.GetAsync("/health/live");
            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        }

        using var rejected = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.True(rejected.Headers.Contains("Retry-After"));
    }
}
//#if LocalAuth

public sealed class AuthenticationRateLimitApiFactory : ApiFactory
{
    protected override Dictionary<string, string?> CreateSettings()
    {
        var settings = base.CreateSettings();
        settings["RateLimiting:AuthenticationPermitLimit"] = "2";
        return settings;
    }
}

/// <summary>Giriş uç noktaları genel sınırdan çok daha sıkı bir sınırla korunur (parola deneme saldırısı).</summary>
public class AuthenticationRateLimitTests(AuthenticationRateLimitApiFactory factory) : IClassFixture<AuthenticationRateLimitApiFactory>
{
    [Fact]
    public async Task RepeatedLoginAttempts_AreRejected()
    {
        using var client = factory.CreateHttpsClient();
        var credentials = new { email = TestUsers.NewEmail(), password = "yanlis-parola-12345" };
        for (var attempt = 0; attempt < 2; attempt++)
        {
            using var allowed = await client.PostAsJsonAsync("/api/auth/login", credentials);
            Assert.Equal(HttpStatusCode.Unauthorized, allowed.StatusCode);
        }

        using var rejected = await client.PostAsJsonAsync("/api/auth/login", credentials);

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
    }
}
//#endif
