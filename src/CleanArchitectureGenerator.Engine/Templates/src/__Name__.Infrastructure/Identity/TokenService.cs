//#if LocalAuth
using System.Buffers.Text;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace __Name__.Infrastructure.Identity;

/// <summary>Erişim token'larını (JWT) ve yenileme token'larını üretir.</summary>
internal sealed class TokenService(IOptions<JwtOptions> options)
{
    private static readonly JsonWebTokenHandler TokenHandler = new();

    /// <summary>
    /// Kısa ömürlü erişim token'ı. Yalnızca gereken talepler yazılır: kullanıcı kimliği (sub) ve tekil kimlik (jti).
    /// E-posta gibi kişisel veriler token'a konmaz; JWT şifreli değildir, herkes içeriğini okuyabilir.
    /// </summary>
    public (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(Guid userId, DateTimeOffset now)
    {
        var jwt = options.Value;
        var expiresAt = now.AddMinutes(jwt.AccessTokenLifetimeMinutes);
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = userId.ToString(),
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)
            },
            SigningCredentials = new SigningCredentials(jwt.CreateSigningKey(), SecurityAlgorithms.HmacSha256)
        };

        return (TokenHandler.CreateToken(descriptor), expiresAt);
    }

    /// <summary>512 bit kriptografik rastgele değer; tahmin edilemez ve URL'de güvenle taşınır.</summary>
    public static string CreateRefreshToken() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(64));

    public static string HashRefreshToken(string refreshToken) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
}
//#endif
