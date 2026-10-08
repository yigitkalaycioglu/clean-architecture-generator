//#if LocalAuth
using Microsoft.IdentityModel.Tokens;

namespace __Name__.Infrastructure.Identity;

/// <summary>
/// Erişim token'larının ayarları ("Jwt" bölümü). İmzalama anahtarı yapılandırma dosyalarında tutulmaz:
/// geliştirmede user-secrets'ta, canlıda ortam değişkeninde (Jwt__SigningKey) ya da bir anahtar kasasındadır.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>HMAC-SHA256 için gereken en kısa anahtar: 256 bit.</summary>
    public const int MinimumSigningKeyBytes = 32;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    /// <summary>En az 32 baytlık rastgele değerin Base64 hali.</summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Kısa tutulur: çalınan bir erişim token'ı en fazla bu süre kullanılabilir.</summary>
    public int AccessTokenLifetimeMinutes { get; set; } = 10;

    public int RefreshTokenLifetimeDays { get; set; } = 14;

    public static bool IsValidSigningKey(string? signingKey)
    {
        if (string.IsNullOrWhiteSpace(signingKey))
        {
            return false;
        }

        var buffer = new byte[signingKey.Length];
        return Convert.TryFromBase64String(signingKey, buffer, out var length) && length >= MinimumSigningKeyBytes;
    }

    internal SymmetricSecurityKey CreateSigningKey() => new(Convert.FromBase64String(SigningKey));

    // Anahtar içerdiği için ToString değerleri göstermez.
    public override string ToString() => nameof(JwtOptions);
}
//#endif
