//#if ExternalAuth
using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace __Name__.IntegrationTests;

/// <summary>Harici kimlik sağlayıcının yerine geçen, testte üretilen RSA imzalı erişim token'ları.</summary>
public static class TestTokens
{
    public const string Issuer = "https://login.test/";
    public const string Audience = "api://test";

    public static RsaSecurityKey SigningKey { get; } = new(RSA.Create(2048)) { KeyId = "test-signing-key" };

    public static string Create(string subject, string audience = Audience, DateTimeOffset? expiresAt = null, SigningCredentials? signingCredentials = null)
    {
        var expires = expiresAt ?? DateTimeOffset.UtcNow.AddMinutes(5);
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = audience,
            Claims = new Dictionary<string, object> { [JwtRegisteredClaimNames.Sub] = subject },
            IssuedAt = expires.AddMinutes(-10).UtcDateTime,
            NotBefore = expires.AddMinutes(-10).UtcDateTime,
            Expires = expires.UtcDateTime,
            SigningCredentials = signingCredentials ?? new SigningCredentials(SigningKey, SecurityAlgorithms.RsaSha256)
        });
    }
}
//#endif
