//#if LocalAuth
using __Name__.Application.Account;
using __Name__.Domain.Common;

namespace __Name__.Application.Abstractions.Authentication;

/// <summary>
/// Kullanıcı hesapları ve oturumlar. Infrastructure'da ASP.NET Core Identity ile uygulanır: parola özetleme,
/// hesap kilitleme, e-posta doğrulama, iki adımlı doğrulama ve yenileme token'ı döngüsü oradadır.
/// </summary>
public interface IIdentityService
{
    Task<Result> RegisterAsync(string email, string password, CancellationToken cancellationToken);

    Task<Result> ConfirmEmailAsync(string userId, string code, CancellationToken cancellationToken);

    Task ResendConfirmationEmailAsync(string email, CancellationToken cancellationToken);

    Task<Result<AuthTokens>> LoginAsync(string email, string password, string? twoFactorCode, string? recoveryCode, CancellationToken cancellationToken);

    Task<Result<AuthTokens>> RefreshAsync(string refreshToken, CancellationToken cancellationToken);

    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken);

    Task ForgotPasswordAsync(string email, CancellationToken cancellationToken);

    Task<Result> ResetPasswordAsync(string email, string code, string newPassword, CancellationToken cancellationToken);

    Task<Result<AccountResponse>> GetAccountAsync(string userId, CancellationToken cancellationToken);

    Task<Result> ChangePasswordAsync(string userId, string currentPassword, string newPassword, CancellationToken cancellationToken);

    Task<Result<TwoFactorSetupResponse>> SetupTwoFactorAsync(string userId, CancellationToken cancellationToken);

    Task<Result<RecoveryCodesResponse>> EnableTwoFactorAsync(string userId, string code, CancellationToken cancellationToken);

    Task<Result> DisableTwoFactorAsync(string userId, string password, CancellationToken cancellationToken);

    Task<Result<RecoveryCodesResponse>> RegenerateRecoveryCodesAsync(string userId, string password, CancellationToken cancellationToken);
}

/// <param name="RefreshToken">Yalnızca HttpOnly çereze yazılır, yanıt gövdesine konmaz.</param>
public sealed record AuthTokens(string AccessToken, DateTimeOffset AccessTokenExpiresAt, string RefreshToken, DateTimeOffset RefreshTokenExpiresAt)
{
    // Token içerdiği için ToString değerleri göstermez; nesne yanlışlıkla loglansa bile token sızmaz.
    public override string ToString() => $"{nameof(AuthTokens)} {{ AccessTokenExpiresAt = {AccessTokenExpiresAt:O} }}";
}

//#endif
