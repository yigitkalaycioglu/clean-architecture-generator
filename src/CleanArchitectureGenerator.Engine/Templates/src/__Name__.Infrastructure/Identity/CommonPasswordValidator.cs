//#if LocalAuth
using System.Collections.Frozen;
using Microsoft.AspNetCore.Identity;

namespace __Name__.Infrastructure.Identity;

/// <summary>
/// Sızdırılmış parola listelerinde en sık görülen parolaları ve e-posta adresini içeren parolaları reddeder
/// (NIST SP 800-63B, 5.1.1.2). Liste örnektir; daha kapsamlı bir liste ya da Have I Been Pwned'in
/// k-anonimlik API'si ile genişletilebilir.
/// </summary>
internal sealed class CommonPasswordValidator : IPasswordValidator<ApplicationUser>
{
    private static readonly FrozenSet<string> CommonPasswords = new[]
    {
        "000000000000", "111111111111", "123123123123", "123412341234", "123456789012", "1234567890123",
        "12345678901234", "123456123456", "123456654321", "123456789abc", "123456789asd", "123qweasdzxc",
        "1q2w3e4r5t6y", "1qaz2wsx3edc", "1234qwerasdf", "a1b2c3d4e5f6", "q1w2e3r4t5y6", "zaq12wsxcde3",
        "qwertyuiop12", "qwertyuiop123", "qwerty123456", "qwertyqwerty", "asdfghjkl123", "zxcvbnm12345",
        "abcdefghijkl", "abc123456789", "abcd12345678", "password1234", "password12345", "passwordpassword",
        "passw0rd1234", "p@ssw0rd1234", "password123!", "iloveyou1234", "welcome12345", "letmein12345",
        "changeme1234", "administrator", "administrator1", "adminadmin123", "princess1234", "sunshine1234",
        "football1234", "baseball1234", "superman1234", "monkey123456", "dragon123456", "trustno1trustno1",
        "galatasaray1905", "fenerbahce1907", "besiktas1903", "trabzonspor1967", "galatasaray123",
        "fenerbahce123", "sifre1234567", "parola123456", "sifresifre12", "turkiye12345"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user, string? password)
    {
        ArgumentNullException.ThrowIfNull(user);
        if (string.IsNullOrEmpty(password))
        {
            return Task.FromResult(IdentityResult.Success);
        }

        if (CommonPasswords.Contains(password.Trim()))
        {
            return Failed("CommonPassword", "Bu parola çok yaygın ve sızdırılmış parola listelerinde bulunuyor; başka bir parola seçin.");
        }

        var emailLocalPart = user.Email?.Split('@')[0];
        if (emailLocalPart is { Length: >= 3 } && password.Contains(emailLocalPart, StringComparison.OrdinalIgnoreCase))
        {
            return Failed("PasswordContainsEmail", "Parola e-posta adresinizi içeremez.");
        }

        return Task.FromResult(IdentityResult.Success);
    }

    private static Task<IdentityResult> Failed(string code, string description) =>
        Task.FromResult(IdentityResult.Failed(new IdentityError { Code = code, Description = description }));
}
//#endif
