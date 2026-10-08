using System.Security.Cryptography;

namespace CleanArchitectureGenerator.Engine;

/// <summary>
/// Her çözüme özel rastgele değerler. Önizleme ile diske yazılan çözümün aynı olması için
/// bir kez üretilip seçeneklerle birlikte taşınır.
/// </summary>
/// <param name="JwtSigningKey">HMAC-SHA256 imzası için 512 bit rastgele anahtar (Base64). Şablonlara değil, user-secrets'a yazılır.</param>
/// <param name="UserSecretsId">Api projesinin user-secrets kimliği.</param>
public sealed record ProjectRandomValues(string JwtSigningKey, int ApiHttpPort, int ApiHttpsPort, Guid UserSecretsId)
{
    public static ProjectRandomValues Create()
    {
        var apiHttpPort = RandomNumberGenerator.GetInt32(5000, 5300);
        var apiHttpsPort = RandomNumberGenerator.GetInt32(7000, 7300);
        var jwtSigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        return new ProjectRandomValues(jwtSigningKey, apiHttpPort, apiHttpsPort, Guid.NewGuid());
    }

    // Anahtar içerdiği için ToString değerleri göstermez.
    public override string ToString() => $"{nameof(ProjectRandomValues)} {{ ApiHttpPort = {ApiHttpPort}, ApiHttpsPort = {ApiHttpsPort} }}";
}
