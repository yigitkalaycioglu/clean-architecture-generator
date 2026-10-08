//#if LocalAuth
using __Name__.Application.Abstractions.Authentication;
using __Name__.Domain.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace __Name__.Infrastructure.Identity;

/// <summary>Giriş, yenileme token'ı döngüsü ve çıkış.</summary>
internal sealed partial class IdentityService
{
    private static string? _dummyPasswordHash;

    public async Task<Result<AuthTokens>> LoginAsync(
        string email, string password, string? twoFactorCode, string? recoveryCode, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            SimulatePasswordCheck(password);
            return IdentityErrors.InvalidCredentials;
        }

        // Kilitli hesapta da aynı yanıt döner; kilit, hesabın var olduğunu ele vermez.
        if (await userManager.IsLockedOutAsync(user))
        {
            SimulatePasswordCheck(password);
            logger.LogWarning("Kilitli hesaba giriş denemesi: {UserId}", user.Id);
            return IdentityErrors.InvalidCredentials;
        }

        if (!await userManager.CheckPasswordAsync(user, password))
        {
            await userManager.AccessFailedAsync(user);
            return IdentityErrors.InvalidCredentials;
        }

        if (!user.EmailConfirmed)
        {
            return IdentityErrors.EmailNotConfirmed;
        }

        if (user.TwoFactorEnabled)
        {
            if (string.IsNullOrWhiteSpace(twoFactorCode) && string.IsNullOrWhiteSpace(recoveryCode))
            {
                return IdentityErrors.TwoFactorRequired;
            }

            var verified = !string.IsNullOrWhiteSpace(twoFactorCode)
                ? await userManager.VerifyTwoFactorTokenAsync(user, userManager.Options.Tokens.AuthenticatorTokenProvider, NormalizeAuthenticatorCode(twoFactorCode))
                : (await userManager.RedeemTwoFactorRecoveryCodeAsync(user, recoveryCode!.Trim().ToUpperInvariant())).Succeeded;

            if (!verified)
            {
                // Hatalı kodlar da hatalı parola gibi sayılır; kod tahmini hesabı kilitler.
                await userManager.AccessFailedAsync(user);
                return IdentityErrors.InvalidTwoFactorCode;
            }
        }

        await userManager.ResetAccessFailedCountAsync(user);
        return await IssueTokensAsync(user.Id, Guid.CreateVersion7(), cancellationToken);
    }

    public async Task<Result<AuthTokens>> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var tokenHash = TokenService.HashRefreshToken(refreshToken);
        var storedToken = await dbContext.RefreshTokens.AsNoTracking()
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
        if (storedToken is null)
        {
            return IdentityErrors.InvalidRefreshToken;
        }

        var now = timeProvider.GetUtcNow();
        if (storedToken.RevokedAt is not null)
        {
            // Kullanılmış bir token yeniden geldi: token çalınmış olabilir (saldırgan ya da kullanıcı eski kopyayı
            // kullanıyor). Hangisinin meşru olduğu bilinemediği için oturum ailesinin tamamı kapatılır.
            await RevokeFamilyAsync(storedToken.FamilyId, now, cancellationToken);
            logger.LogWarning("Kullanılmış yenileme token'ı tekrar gönderildi, oturum ailesi kapatıldı: {UserId}", storedToken.UserId);
            return IdentityErrors.InvalidRefreshToken;
        }

        if (storedToken.ExpiresAt <= now)
        {
            return IdentityErrors.InvalidRefreshToken;
        }

        // Token atomik olarak "kullanıldı" işaretlenir: aynı token'la gelen eşzamanlı iki istekten yalnızca biri geçer.
        var claimed = await dbContext.RefreshTokens
            .Where(token => token.Id == storedToken.Id && token.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAt, (DateTimeOffset?)now), cancellationToken);
        if (claimed == 0)
        {
            await RevokeFamilyAsync(storedToken.FamilyId, now, cancellationToken);
            return IdentityErrors.InvalidRefreshToken;
        }

        var user = await userManager.FindByIdAsync(storedToken.UserId.ToString());
        if (user is null || await userManager.IsLockedOutAsync(user))
        {
            await RevokeFamilyAsync(storedToken.FamilyId, now, cancellationToken);
            return IdentityErrors.InvalidRefreshToken;
        }

        return await IssueTokensAsync(user.Id, storedToken.FamilyId, cancellationToken);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var tokenHash = TokenService.HashRefreshToken(refreshToken);
        var familyId = await dbContext.RefreshTokens
            .Where(token => token.TokenHash == tokenHash)
            .Select(token => (Guid?)token.FamilyId)
            .FirstOrDefaultAsync(cancellationToken);

        if (familyId is { } id)
        {
            await RevokeFamilyAsync(id, timeProvider.GetUtcNow(), cancellationToken);
        }
    }

    private async Task<AuthTokens> IssueTokensAsync(Guid userId, Guid familyId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var (accessToken, accessTokenExpiresAt) = tokenService.CreateAccessToken(userId, now);
        var refreshToken = TokenService.CreateRefreshToken();
        var refreshTokenExpiresAt = now.AddDays(jwtOptions.Value.RefreshTokenLifetimeDays);

        // Süresi dolmuş eski kayıtlar temizlenir; tablo kullanıcı başına sınırsız büyümez.
        await dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.ExpiresAt < now)
            .ExecuteDeleteAsync(cancellationToken);

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = userId,
            FamilyId = familyId,
            TokenHash = TokenService.HashRefreshToken(refreshToken),
            CreatedAt = now,
            ExpiresAt = refreshTokenExpiresAt
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthTokens(accessToken, accessTokenExpiresAt, refreshToken, refreshTokenExpiresAt);
    }

    private Task RevokeFamilyAsync(Guid familyId, DateTimeOffset now, CancellationToken cancellationToken) =>
        dbContext.RefreshTokens
            .Where(token => token.FamilyId == familyId && token.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAt, (DateTimeOffset?)now), cancellationToken);

    private Task RevokeAllSessionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        return dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAt, (DateTimeOffset?)now), cancellationToken);
    }

    /// <summary>
    /// Kullanıcı yoksa da bir parola özeti doğrulanır; böylece yanıt süresi e-postanın kayıtlı olup olmadığını ele vermez.
    /// </summary>
    private void SimulatePasswordCheck(string password)
    {
        var hash = _dummyPasswordHash ??= userManager.PasswordHasher.HashPassword(new ApplicationUser(), Guid.NewGuid().ToString());
        userManager.PasswordHasher.VerifyHashedPassword(new ApplicationUser(), hash, password);
    }

    private static string NormalizeAuthenticatorCode(string code) =>
        code.Replace(" ", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);
}
//#endif
