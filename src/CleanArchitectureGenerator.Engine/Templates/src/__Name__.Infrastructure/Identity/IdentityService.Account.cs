//#if LocalAuth
using System.Text;
using __Name__.Application.Abstractions.Authentication;
using __Name__.Application.Account;
using __Name__.Domain.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace __Name__.Infrastructure.Identity;

/// <summary>Hesap bilgileri, parola değiştirme ve iki adımlı doğrulama.</summary>
internal sealed partial class IdentityService
{
    private const int RecoveryCodeCount = 10;

    public async Task<Result<AccountResponse>> GetAccountAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await FindByIdAsync(userId);
        if (user is null)
        {
            return IdentityErrors.UserNotFound;
        }

        var recoveryCodesLeft = user.TwoFactorEnabled ? await userManager.CountRecoveryCodesAsync(user) : 0;
        return new AccountResponse(user.Id.ToString(), user.Email!, user.EmailConfirmed, user.TwoFactorEnabled, recoveryCodesLeft);
    }

    public async Task<Result> ChangePasswordAsync(string userId, string currentPassword, string newPassword, CancellationToken cancellationToken)
    {
        var user = await FindByIdAsync(userId);
        if (user is null)
        {
            return IdentityErrors.UserNotFound;
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return IdentityErrors.PasswordMismatch;
        }

        var changed = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!changed.Succeeded)
        {
            if (changed.Errors.Any(error => error.Code == nameof(IdentityErrorDescriber.PasswordMismatch)))
            {
                // Çalınmış bir oturumla parola tahmini yapılamasın diye hatalı denemeler hesabı kilitler.
                await userManager.AccessFailedAsync(user);
                return IdentityErrors.PasswordMismatch;
            }

            return ToValidationError("newPassword", changed);
        }

        await userManager.ResetAccessFailedCountAsync(user);
        await RevokeAllSessionsAsync(user.Id, cancellationToken);
        logger.LogInformation("Parola değiştirildi, tüm oturumlar kapatıldı: {UserId}", user.Id);
        return Result.Success();
    }

    public async Task<Result<TwoFactorSetupResponse>> SetupTwoFactorAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await FindByIdAsync(userId);
        if (user is null)
        {
            return IdentityErrors.UserNotFound;
        }

        if (user.TwoFactorEnabled)
        {
            return IdentityErrors.TwoFactorAlreadyEnabled;
        }

        await userManager.ResetAuthenticatorKeyAsync(user);
        var key = await userManager.GetAuthenticatorKeyAsync(user) ?? string.Empty;
        var issuer = Uri.EscapeDataString(jwtOptions.Value.Issuer);
        var account = Uri.EscapeDataString(user.Email!);
        var authenticatorUri = $"otpauth://totp/{issuer}:{account}?secret={key}&issuer={issuer}&digits=6";

        return new TwoFactorSetupResponse(FormatKey(key), authenticatorUri);
    }

    public async Task<Result<RecoveryCodesResponse>> EnableTwoFactorAsync(string userId, string code, CancellationToken cancellationToken)
    {
        var user = await FindByIdAsync(userId);
        if (user is null)
        {
            return IdentityErrors.UserNotFound;
        }

        if (user.TwoFactorEnabled)
        {
            return IdentityErrors.TwoFactorAlreadyEnabled;
        }

        var hasKey = !string.IsNullOrEmpty(await userManager.GetAuthenticatorKeyAsync(user));
        var verified = hasKey && await userManager.VerifyTwoFactorTokenAsync(
            user, userManager.Options.Tokens.AuthenticatorTokenProvider, NormalizeAuthenticatorCode(code));
        if (!verified)
        {
            return IdentityErrors.InvalidVerificationCode;
        }

        await userManager.SetTwoFactorEnabledAsync(user, true);
        var recoveryCodes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, RecoveryCodeCount);
        logger.LogInformation("İki adımlı doğrulama açıldı: {UserId}", user.Id);
        return new RecoveryCodesResponse([.. recoveryCodes ?? []]);
    }

    public async Task<Result> DisableTwoFactorAsync(string userId, string password, CancellationToken cancellationToken)
    {
        var user = await FindByIdAsync(userId);
        if (user is null)
        {
            return IdentityErrors.UserNotFound;
        }

        if (!user.TwoFactorEnabled)
        {
            return IdentityErrors.TwoFactorNotEnabled;
        }

        if (!await CheckPasswordWithLockoutAsync(user, password))
        {
            return IdentityErrors.PasswordMismatch;
        }

        // Eski anahtar ve kurtarma kodları da geçersiz kılınır; yeniden açılırsa yeni anahtar gerekir.
        await userManager.SetTwoFactorEnabledAsync(user, false);
        await userManager.ResetAuthenticatorKeyAsync(user);
        await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 0);
        logger.LogWarning("İki adımlı doğrulama kapatıldı: {UserId}", user.Id);
        return Result.Success();
    }

    public async Task<Result<RecoveryCodesResponse>> RegenerateRecoveryCodesAsync(string userId, string password, CancellationToken cancellationToken)
    {
        var user = await FindByIdAsync(userId);
        if (user is null)
        {
            return IdentityErrors.UserNotFound;
        }

        if (!user.TwoFactorEnabled)
        {
            return IdentityErrors.TwoFactorNotEnabled;
        }

        if (!await CheckPasswordWithLockoutAsync(user, password))
        {
            return IdentityErrors.PasswordMismatch;
        }

        var recoveryCodes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, RecoveryCodeCount);
        return new RecoveryCodesResponse([.. recoveryCodes ?? []]);
    }

    /// <summary>Hassas işlemlerden önce parolayı yeniden doğrular; hatalı denemeler hesabı kilitler.</summary>
    private async Task<bool> CheckPasswordWithLockoutAsync(ApplicationUser user, string password)
    {
        if (await userManager.IsLockedOutAsync(user))
        {
            return false;
        }

        if (await userManager.CheckPasswordAsync(user, password))
        {
            await userManager.ResetAccessFailedCountAsync(user);
            return true;
        }

        await userManager.AccessFailedAsync(user);
        return false;
    }

    /// <summary>Anahtarı okunması kolay dörtlü gruplara böler: "abcd efgh ijkl…".</summary>
    private static string FormatKey(string key)
    {
        var builder = new StringBuilder();
        for (var index = 0; index < key.Length; index += 4)
        {
            builder.Append(key.AsSpan(index, Math.Min(4, key.Length - index))).Append(' ');
        }

        return builder.ToString().TrimEnd().ToLowerInvariant();
    }
}
//#endif
