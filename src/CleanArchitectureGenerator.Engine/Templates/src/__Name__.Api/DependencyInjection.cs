using System.Globalization;
using System.Net;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using __Name__.Api.Common;
using __Name__.Api.OpenApi;
using __Name__.Application.Abstractions.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;

namespace __Name__.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, CurrentUser>();

        // Hatalar RFC 9457 ProblemDetails biçiminde döner; iç ayrıntı (yığın izi, SQL) asla istemciye gitmez.
        // Her yanıttaki traceId, loglardaki kayıtla eşleştirme içindir.
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.ConfigureHttpJsonOptions(options =>
        {
            // Beklenmeyen alan içeren gövdeler reddedilir (ör. "ownerId" göndererek kayıt sahibini değiştirme denemesi).
            options.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
            options.SerializerOptions.MaxDepth = 32;
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        // Varsayılan olarak her uç nokta kimlik doğrulaması ister; anonim erişim yalnızca AllowAnonymous ile açılır.
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        services.AddCors(options => options.AddDefaultPolicy(policy =>
        {
            // Yalnızca listelenen kaynaklar (origin) API'yi tarayıcıdan çağırabilir; liste boşsa hiçbiri.
            var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
            if (allowedOrigins.Length > 0)
            {
                policy.WithOrigins(allowedOrigins)
                    .WithMethods(HttpMethods.Get, HttpMethods.Post, HttpMethods.Put, HttpMethods.Delete)
                    .WithHeaders("Authorization", "Content-Type")
                    .AllowCredentials()
                    .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
            }
        }));

        services.AddRateLimiting(configuration);

        services.AddRequestTimeouts(options => options.DefaultPolicy = new RequestTimeoutPolicy
        {
            Timeout = TimeSpan.FromSeconds(30),
            TimeoutStatusCode = StatusCodes.Status504GatewayTimeout
        });

        // IIS'te barındırıldığında da aynı gövde sınırı geçerli olur (Kestrel sınırı Program.cs'te).
        services.Configure<IISServerOptions>(options => options.MaxRequestBodySize = ApiLimits.MaxRequestBodySize);

        services.AddHsts(options =>
        {
            options.MaxAge = TimeSpan.FromDays(365);
            options.IncludeSubDomains = true;
        });

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            // Yalnızca güvenilen ters vekil sunuculardan gelen X-Forwarded-* başlıkları dikkate alınır; aksi halde
            // istemci sahte bir IP göndererek hız sınırını aşabilirdi. Varsayılan olarak yalnızca yerel makine güvenilir.
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;
            foreach (var proxy in configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [])
            {
                if (IPAddress.TryParse(proxy, out var address))
                {
                    options.KnownProxies.Add(address);
                }
            }
        });

        services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

        return services;
    }

    private static void AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRateLimiter(options =>
        {
            var settings = configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>() ?? new RateLimitingOptions();
            var window = TimeSpan.FromSeconds(settings.WindowSeconds);
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Her istemci (IP) için genel sınır.
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(GetClientKey(context), _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = settings.PermitLimit,
                    Window = window,
                    QueueLimit = 0
                }));

            // Giriş, kayıt ve parola işlemleri için çok daha sıkı sınır: parola deneme ve e-posta bombardımanına karşı.
            options.AddPolicy(RateLimitPolicies.Authentication, context =>
                RateLimitPartition.GetFixedWindowLimiter(GetClientKey(context), _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = settings.AuthenticationPermitLimit,
                    Window = window,
                    QueueLimit = 0
                }));

            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                }

                var problemDetails = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
                await problemDetails.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context.HttpContext,
                    ProblemDetails = { Status = StatusCodes.Status429TooManyRequests, Detail = "Çok fazla istek gönderildi, bir süre sonra yeniden deneyin." }
                });
            };
        });
    }

    private static string GetClientKey(HttpContext context) => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
