//#if LocalAuth
using System.Security.Cryptography;
using __Name__.Application.Abstractions.Email;
//#endif
using __Name__.Infrastructure.Persistence;
//#if ExternalAuth
using Microsoft.AspNetCore.Authentication.JwtBearer;
//#endif
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
//#if ExternalAuth
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
//#endif

namespace __Name__.IntegrationTests;

/// <summary>
/// Uygulamayı bellekte gerçek HTTP hattıyla çalıştırır. Seçilen veritabanı sağlayıcısının yerine bellekte SQLite
/// kullanılır; her test sınıfı (fixture) kendi boş veritabanını alır.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
//#if LocalAuth

    public TestEmailSender Emails { get; } = new();
//#endif

    /// <summary>Testlerin yapılandırması. Hız sınırları, testler birbirini engellemesin diye çok yüksek tutulur.</summary>
    protected virtual Dictionary<string, string?> CreateSettings() => new()
    {
//#if LocalAuth
        ["Jwt:SigningKey"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
        ["Frontend:BaseUrl"] = TestEmailSender.FrontendBaseUrl,
        ["Smtp:Host"] = "localhost",
        ["Smtp:From"] = "noreply@test.local",
//#else
        ["Authentication:Authority"] = TestTokens.Issuer,
        ["Authentication:Audience"] = TestTokens.Audience,
//#endif
        ["RateLimiting:PermitLimit"] = "100000",
        ["RateLimiting:AuthenticationPermitLimit"] = "100000"
    };

    public HttpClient CreateHttpsClient(bool handleCookies = true) =>
        CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), HandleCookies = handleCookies });

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreatedAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _connection.DisposeAsync();
        await DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        foreach (var (key, value) in CreateSettings())
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureTestServices(services =>
        {
            // Gerçek veritabanı kaydı kaldırılır, yerine bellekteki SQLite bağlantısı konur.
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
            {
                options.UseSqlite(_connection);
                options.AddInterceptors(serviceProvider.GetServices<ISaveChangesInterceptor>());
            });
//#if LocalAuth

            // E-postalar gönderilmez, testlerin okuyabilmesi için bellekte tutulur.
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Emails);
//#else

            // Kimlik sağlayıcıya gidilmez: imza anahtarı ve yayıncı, test token'larınınkiyle sabitlenir.
            services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Configuration = new OpenIdConnectConfiguration { Issuer = TestTokens.Issuer };
                options.Configuration.SigningKeys.Add(TestTokens.SigningKey);
            });
//#endif
        });
    }
}
