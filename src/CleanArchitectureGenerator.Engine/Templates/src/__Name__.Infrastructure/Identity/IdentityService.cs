//#if LocalAuth
using System.Buffers.Text;
using System.Text;
using __Name__.Application.Abstractions.Authentication;
using __Name__.Application.Abstractions.Email;
using __Name__.Domain.Common;
using __Name__.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace __Name__.Infrastructure.Identity;

/// <summary>
/// Hesap işlemlerinin ASP.NET Core Identity ile uygulanması. Kayıt, e-posta doğrulama ve parola sıfırlama bu
/// dosyada; oturumlar IdentityService.Sessions.cs, hesap ve iki adımlı doğrulama IdentityService.Account.cs içinde.
/// </summary>
internal sealed partial class IdentityService(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext,
    TokenService tokenService,
    IEmailSender emailSender,
    IOptions<JwtOptions> jwtOptions,
    IOptions<FrontendOptions> frontendOptions,
    TimeProvider timeProvider,
    ILogger<IdentityService> logger) : IIdentityService
{
    public async Task<Result> RegisterAsync(string email, string password, CancellationToken cancellationToken)
    {
        var user = new ApplicationUser { UserName = email, Email = email };

        // Parola politikası, adresin kayıtlı olup olmadığına bakılmadan önce denetlenir; aksi halde yanıtlar
        // arasındaki fark adresin kayıtlı olduğunu ele verirdi.
        var passwordCheck = await ValidatePasswordAsync(user, password);
        if (passwordCheck.IsFailure)
        {
            return passwordCheck;
        }

        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            // Yeni kayıtta parola özetlendiği için burada da özetlenir; yanıt süresi adresin kayıtlı olduğunu ele vermez.
            _ = userManager.PasswordHasher.HashPassword(user, password);
            await SendEmailAsync(
                existingUser,
                "Hesap kaydı denemesi",
                "Bu e-posta adresiyle yeni bir hesap açılmak istendi, ancak adres zaten kayıtlı.\n\n" +
                "Bu siz değilseniz bu e-postayı dikkate almayın. Parolanızı unuttuysanız \"Parolamı unuttum\" ile sıfırlayabilirsiniz.",
                cancellationToken);
            return Result.Success();
        }

        var created = await userManager.CreateAsync(user, password);
        if (!created.Succeeded)
        {
            // Aynı adresle eşzamanlı iki kayıt: veritabanındaki benzersiz indeks ikincisini reddeder, yanıt yine aynıdır.
            return created.Errors.Any(error => error.Code is nameof(IdentityErrorDescriber.DuplicateUserName) or nameof(IdentityErrorDescriber.DuplicateEmail))
                ? Result.Success()
                : ToValidationError("email", created);
        }

        logger.LogInformation("Yeni kullanıcı kaydı: {UserId}", user.Id);
        await SendConfirmationEmailAsync(user, cancellationToken);
        return Result.Success();
    }

    public async Task<Result> ConfirmEmailAsync(string userId, string code, CancellationToken cancellationToken)
    {
        var user = await FindByIdAsync(userId);
        if (user is null || !TryDecodeCode(code, out var token))
        {
            return IdentityErrors.InvalidCode;
        }

        if (user.EmailConfirmed)
        {
            return Result.Success();
        }

        var confirmed = await userManager.ConfirmEmailAsync(user, token);
        return confirmed.Succeeded ? Result.Success() : IdentityErrors.InvalidCode;
    }

    public async Task ResendConfirmationEmailAsync(string email, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is { EmailConfirmed: false })
        {
            await SendConfirmationEmailAsync(user, cancellationToken);
        }
    }

    public async Task ForgotPasswordAsync(string email, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is not { EmailConfirmed: true })
        {
            return;
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var link = frontendOptions.Value.BuildLink("reset-password", new Dictionary<string, string?>
        {
            ["email"] = user.Email,
            ["code"] = EncodeCode(token)
        });

        await SendEmailAsync(
            user,
            "Parola sıfırlama",
            $"Parolanızı sıfırlamak için bağlantıyı açın:\n{link}\n\nBağlantı kısa süre geçerlidir ve bir kez kullanılabilir. " +
            "Bu isteği siz yapmadıysanız bu e-postayı dikkate almayın; parolanız değişmez.",
            cancellationToken);
    }

    public async Task<Result> ResetPasswordAsync(string email, string code, string newPassword, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null || !TryDecodeCode(code, out var token))
        {
            return IdentityErrors.InvalidCode;
        }

        var reset = await userManager.ResetPasswordAsync(user, token, newPassword);
        if (!reset.Succeeded)
        {
            return reset.Errors.Any(error => error.Code == nameof(IdentityErrorDescriber.InvalidToken))
                ? IdentityErrors.InvalidCode
                : ToValidationError("newPassword", reset);
        }

        // E-posta sahipliği kanıtlandı: kilit kaldırılır ve parola değiştiği için tüm açık oturumlar kapatılır.
        await userManager.SetLockoutEndDateAsync(user, null);
        await userManager.ResetAccessFailedCountAsync(user);
        await RevokeAllSessionsAsync(user.Id, cancellationToken);
        logger.LogInformation("Parola sıfırlandı, tüm oturumlar kapatıldı: {UserId}", user.Id);
        return Result.Success();
    }

    private async Task SendConfirmationEmailAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var link = frontendOptions.Value.BuildLink("confirm-email", new Dictionary<string, string?>
        {
            ["userId"] = user.Id.ToString(),
            ["code"] = EncodeCode(token)
        });

        await SendEmailAsync(
            user,
            "E-posta adresinizi doğrulayın",
            $"Hesabınızı etkinleştirmek için bağlantıyı açın:\n{link}\n\nBu kaydı siz yapmadıysanız bu e-postayı dikkate almayın.",
            cancellationToken);
    }

    private ValueTask SendEmailAsync(ApplicationUser user, string subject, string body, CancellationToken cancellationToken) =>
        emailSender.SendAsync(new EmailMessage(user.Email!, subject, body), cancellationToken);

    private async Task<Result> ValidatePasswordAsync(ApplicationUser user, string password)
    {
        var errors = new List<IdentityError>();
        foreach (var validator in userManager.PasswordValidators)
        {
            var result = await validator.ValidateAsync(userManager, user, password);
            errors.AddRange(result.Errors);
        }

        return errors.Count == 0 ? Result.Success() : ToValidationError("password", IdentityResult.Failed([.. errors]));
    }

    private async Task<ApplicationUser?> FindByIdAsync(string userId) =>
        Guid.TryParse(userId, out var id) ? await userManager.FindByIdAsync(id.ToString()) : null;

    private static Error ToValidationError(string field, IdentityResult result) =>
        Error.Validation(new Dictionary<string, string[]>
        {
            [field] = result.Errors.Select(error => error.Description).Distinct(StringComparer.Ordinal).ToArray()
        });

    /// <summary>Identity kodları '+', '/' gibi karakterler içerir; bağlantıda bozulmasınlar diye Base64Url ile kodlanır.</summary>
    private static string EncodeCode(string token) => Base64Url.EncodeToString(Encoding.UTF8.GetBytes(token));

    private static bool TryDecodeCode(string code, out string token)
    {
        token = string.Empty;
        if (!Base64Url.IsValid(code))
        {
            return false;
        }

        token = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(code));
        return true;
    }
}
//#endif
