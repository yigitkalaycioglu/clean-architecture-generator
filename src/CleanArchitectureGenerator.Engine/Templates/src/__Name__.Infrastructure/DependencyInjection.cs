using __Name__.Application.Abstractions.Data;
//#if LocalAuth
using __Name__.Application.Abstractions.Authentication;
using __Name__.Application.Abstractions.Email;
using __Name__.Application.Authentication;
using __Name__.Infrastructure.Email;
using __Name__.Infrastructure.Identity;
//#else
using __Name__.Infrastructure.Authentication;
//#endif
using __Name__.Infrastructure.Persistence;
using __Name__.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Authentication.JwtBearer;
//#if LocalAuth
using Microsoft.AspNetCore.Identity;
//#endif
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
//#if LocalAuth
using Microsoft.IdentityModel.JsonWebTokens;
//#endif
using Microsoft.IdentityModel.Tokens;

namespace __Name__.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddPersistence();
//#if LocalAuth
        services.AddIdentityServices(configuration, environment);
//#else
        services.AddExternalAuthentication(configuration, environment);
//#endif
        return services;
    }

    private static void AddPersistence(this IServiceCollection services)
    {
        // Kaydetme kesicileri: önce alan olayları işlenir, sonra denetim alanları doldurulur
        // (böylece olay işleyicilerinin yaptığı değişiklikler de denetim bilgisi alır).
        services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();
        services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();

        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            var connectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "'ConnectionStrings:DefaultConnection' ayarı bulunamadı. Geliştirmede appsettings.Development.json ya da " +
                    "user-secrets, canlıda ortam değişkeni (ConnectionStrings__DefaultConnection) kullanın.");
            }

            options.__DbUseMethod__(connectionString);
            options.AddInterceptors(serviceProvider.GetServices<ISaveChangesInterceptor>());
        });

        services.AddScoped<IApplicationDbContext>(serviceProvider => serviceProvider.GetRequiredService<ApplicationDbContext>());

        services.AddHealthChecks().AddDbContextCheck<ApplicationDbContext>("database", tags: ["ready"]);
    }
//#if LocalAuth

    private static void AddIdentityServices(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(jwt => !string.IsNullOrWhiteSpace(jwt.Issuer) && !string.IsNullOrWhiteSpace(jwt.Audience), "Jwt:Issuer ve Jwt:Audience ayarlanmalı.")
            .Validate(
                jwt => JwtOptions.IsValidSigningKey(jwt.SigningKey),
                $"Jwt:SigningKey en az {JwtOptions.MinimumSigningKeyBytes} baytlık rastgele bir değerin Base64 hali olmalı. " +
                "Geliştirmede user-secrets'a, canlıda ortam değişkenine (Jwt__SigningKey) ya da anahtar kasasına yazın.")
            .Validate(jwt => jwt.AccessTokenLifetimeMinutes is >= 1 and <= 60, "Jwt:AccessTokenLifetimeMinutes 1-60 arasında olmalı.")
            .Validate(jwt => jwt.RefreshTokenLifetimeDays is >= 1 and <= 90, "Jwt:RefreshTokenLifetimeDays 1-90 arasında olmalı.")
            .ValidateOnStart();

        services.AddOptions<FrontendOptions>()
            .Bind(configuration.GetSection(FrontendOptions.SectionName))
            .Validate(
                frontend => Uri.TryCreate(frontend.BaseUrl, UriKind.Absolute, out var uri) &&
                    (uri.Scheme == Uri.UriSchemeHttps || (environment.IsDevelopment() && uri.Scheme == Uri.UriSchemeHttp)),
                "Frontend:BaseUrl, e-postalardaki bağlantılar için istemci uygulamanın https adresi olmalı.")
            .ValidateOnStart();

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.User.AllowedUserNameCharacters = string.Empty;
                options.SignIn.RequireConfirmedEmail = true;

                // NIST SP 800-63B: karmaşıklık kuralları yerine uzunluk; sık kullanılan parolalar ayrıca reddedilir.
                options.Password.RequiredLength = AuthenticationRules.PasswordMinLength;
                options.Password.RequiredUniqueChars = 4;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;

                // Kaba kuvvet koruması: 5 hatalı denemeden sonra hesap 15 dakika kilitlenir.
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders()
            .AddErrorDescriber<TurkishIdentityErrorDescriber>()
            .AddPasswordValidator<CommonPasswordValidator>();

        // OWASP Password Storage Cheat Sheet: PBKDF2-HMAC-SHA512 için en az 210.000 tekrar.
        services.Configure<PasswordHasherOptions>(options => options.IterationCount = 210_000);

        // E-posta doğrulama ve parola sıfırlama bağlantıları 2 saat geçerlidir.
        services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromHours(2));

        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<TokenService>();

        services.AddSingleton<EmailQueue>();
        services.AddSingleton<IEmailSender>(serviceProvider => serviceProvider.GetRequiredService<EmailQueue>());
        services.AddHostedService<EmailDispatcher>();
        if (environment.IsDevelopment())
        {
            services.AddSingleton<IEmailTransport, DevelopmentEmailTransport>();
        }
        else
        {
            services.AddOptions<SmtpOptions>()
                .Bind(configuration.GetSection(SmtpOptions.SectionName))
                .Validate(smtp => smtp.IsValid(), "Smtp:Host, Smtp:Port ve Smtp:From ayarlanmalı (Development dışında e-posta gönderimi zorunludur).")
                .ValidateOnStart();
            services.AddSingleton<IEmailTransport, SmtpEmailTransport>();
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = jwt.CreateSigningKey(),

                    // Yalnızca bu algoritma kabul edilir: "alg: none" ve algoritma karıştırma saldırıları reddedilir.
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    NameClaimType = JwtRegisteredClaimNames.Sub,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });
    }
//#else

    private static void AddExternalAuthentication(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        // Development'ta sağlayıcı henüz ayarlanmamış olabilir: uygulama açılır ama hiçbir token kabul edilmez.
        // Diğer ortamlarda ayarlar zorunludur ve sağlayıcı adresi https olmalıdır.
        services.AddOptions<ExternalAuthenticationOptions>()
            .Bind(configuration.GetSection(ExternalAuthenticationOptions.SectionName))
            .Validate(
                external => environment.IsDevelopment() || (external.IsConfigured && external.HasSecureAuthority),
                "Authentication:Authority (https) ve Authentication:Audience ayarlanmalı.")
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<ExternalAuthenticationOptions>>((bearer, externalOptions) =>
            {
                var external = externalOptions.Value;
                if (!string.IsNullOrWhiteSpace(external.Authority))
                {
                    bearer.Authority = external.Authority;
                }

                bearer.Audience = external.Audience;
                bearer.RequireHttpsMetadata = !environment.IsDevelopment();
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters.NameClaimType = "sub";
                bearer.TokenValidationParameters.ClockSkew = TimeSpan.FromSeconds(30);

                // Yalnızca asimetrik imzalar kabul edilir: sağlayıcının açık anahtarıyla HMAC token'ı üretilemez
                // (algoritma karıştırma saldırısı) ve "alg: none" reddedilir.
                bearer.TokenValidationParameters.ValidAlgorithms =
                [
                    SecurityAlgorithms.RsaSha256, SecurityAlgorithms.RsaSha384, SecurityAlgorithms.RsaSha512,
                    SecurityAlgorithms.RsaSsaPssSha256, SecurityAlgorithms.RsaSsaPssSha384, SecurityAlgorithms.RsaSsaPssSha512,
                    SecurityAlgorithms.EcdsaSha256, SecurityAlgorithms.EcdsaSha384, SecurityAlgorithms.EcdsaSha512
                ];
            });
    }
//#endif
}
