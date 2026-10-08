namespace __Name__.Api.Common;

/// <summary>
/// Her yanıta tarayıcı güvenlik başlıklarını ekler (OWASP Secure Headers). API yalnızca JSON döndürdüğü için
/// içerik politikası her şeyi yasaklar: yanıt bir sayfada çalıştırılamaz, çerçeveye alınamaz, önbelleğe yazılmaz.
/// </summary>
public static class SecurityHeaders
{
    private const string ApiContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";

    private const string PermissionsPolicy =
        "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()";

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);

        return app.Use(async (context, next) =>
        {
            // Başlıklar yanıt gönderilmeden hemen önce yazılır; hata sayfaları ve 401/404 gibi yanıtlar da dahil.
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers.XContentTypeOptions = "nosniff";
                headers.XFrameOptions = "DENY";
                headers["Referrer-Policy"] = "no-referrer";
                headers["Permissions-Policy"] = PermissionsPolicy;
                headers["Cross-Origin-Opener-Policy"] = "same-origin";
                headers["Cross-Origin-Resource-Policy"] = "same-origin";

                // Swagger UI (yalnızca Development) kendi betik ve stillerini yükler; katı politika ona uygulanmaz.
                var isSwaggerUi = environment.IsDevelopment() && context.Request.Path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase);
                if (!isSwaggerUi)
                {
                    headers.ContentSecurityPolicy = ApiContentSecurityPolicy;
                }

                // Yanıtlar kişisel veri ya da token içerebilir: tarayıcı ve ara sunucular saklamaz.
                if (string.IsNullOrEmpty(headers.CacheControl))
                {
                    headers.CacheControl = "no-store";
                }

                return Task.CompletedTask;
            });

            await next(context);
        });
    }
}
