//#if LocalAuth
using __Name__.Api.Common;
using __Name__.Application.Abstractions.Authentication;
using __Name__.Application.Abstractions.Messaging;
using __Name__.Application.Authentication;

namespace __Name__.Api.Endpoints;

/// <summary>
/// Kayıt, giriş ve oturum uç noktaları. Hepsi anonimdir ve sıkı hız sınırına tabidir. Erişim token'ı yanıt
/// gövdesinde döner (istemci bellekte tutar); yenileme token'ı JavaScript'in okuyamadığı bir çerezdedir.
/// </summary>
public static class AuthEndpoints
{
    /// <summary>__Secure- öneki: tarayıcı bu çerezi yalnızca HTTPS üzerinden ve Secure özelliğiyle kabul eder.</summary>
    public const string RefreshTokenCookie = "__Secure-refresh-token";

    /// <summary>Çerez yalnızca bu yoldaki isteklere eklenir; diğer API çağrılarında hiç gönderilmez.</summary>
    private const string RefreshTokenCookiePath = "/api/auth";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth")
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.Authentication);

        group.MapPost("/register", async (RegisterCommand command, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.IsSuccess
                ? Results.Accepted(value: new MessageResponse("Kayıt alındı. E-posta adresinize gönderilen bağlantıyla hesabınızı doğrulayın."))
                : result.ToProblem();
        });

        group.MapPost("/confirm-email", async (ConfirmEmailCommand command, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.IsSuccess ? Results.NoContent() : result.ToProblem();
        });

        group.MapPost("/resend-confirmation-email", async (ResendConfirmationEmailCommand command, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.IsSuccess
                ? Results.Accepted(value: new MessageResponse("Adres kayıtlı ve doğrulanmamışsa yeni bir bağlantı gönderildi."))
                : result.ToProblem();
        });

        group.MapPost("/login", async (LoginCommand command, ISender sender, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.IsSuccess ? StartSession(httpContext, result.Value) : result.ToProblem();
        });

        group.MapPost("/refresh", async (ISender sender, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            var refreshToken = httpContext.Request.Cookies[RefreshTokenCookie];
            if (string.IsNullOrEmpty(refreshToken))
            {
                return IdentityErrors.InvalidRefreshToken.ToProblem();
            }

            var result = await sender.Send(new RefreshSessionCommand(refreshToken), cancellationToken);
            if (result.IsFailure)
            {
                DeleteRefreshTokenCookie(httpContext);
                return result.ToProblem();
            }

            return StartSession(httpContext, result.Value);
        });

        group.MapPost("/logout", async (ISender sender, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            await sender.Send(new LogoutCommand(httpContext.Request.Cookies[RefreshTokenCookie]), cancellationToken);
            DeleteRefreshTokenCookie(httpContext);
            return Results.NoContent();
        });

        group.MapPost("/forgot-password", async (ForgotPasswordCommand command, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.IsSuccess
                ? Results.Accepted(value: new MessageResponse("Adres kayıtlıysa parola sıfırlama bağlantısı gönderildi."))
                : result.ToProblem();
        });

        group.MapPost("/reset-password", async (ResetPasswordCommand command, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.IsSuccess ? Results.NoContent() : result.ToProblem();
        });

        return app;
    }

    private static IResult StartSession(HttpContext httpContext, AuthTokens tokens)
    {
        httpContext.Response.Cookies.Append(RefreshTokenCookie, tokens.RefreshToken, CreateCookieOptions(tokens.RefreshTokenExpiresAt));
        return Results.Ok(new AccessTokenResponse(tokens.AccessToken, tokens.AccessTokenExpiresAt));
    }

    private static void DeleteRefreshTokenCookie(HttpContext httpContext) =>
        httpContext.Response.Cookies.Delete(RefreshTokenCookie, CreateCookieOptions(expires: null));

    /// <summary>
    /// HttpOnly: JavaScript (ve dolayısıyla XSS) çerezi okuyamaz. Secure: yalnızca HTTPS. SameSite=Strict: başka bir
    /// siteden tetiklenen isteklere eklenmez (CSRF koruması).
    /// </summary>
    private static CookieOptions CreateCookieOptions(DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = RefreshTokenCookiePath,
        Expires = expires,
        IsEssential = true
    };
}

public sealed record AccessTokenResponse(string AccessToken, DateTimeOffset ExpiresAt)
{
    public string TokenType => "Bearer";

    public override string ToString() => $"{nameof(AccessTokenResponse)} {{ ExpiresAt = {ExpiresAt:O} }}";
}

public sealed record MessageResponse(string Message);
//#endif
