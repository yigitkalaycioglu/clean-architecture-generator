using System.Security.Cryptography;

namespace NTierGenerator.Engine;

/// <summary>
/// Her çözüme özel rastgele değerler. Önizleme ile diske yazılan çözümün aynı olması için
/// bir kez üretilip seçeneklerle birlikte taşınır.
/// </summary>
/// <param name="JwtSecret">HS512 imzası için 512 bitten uzun JWT anahtarı.</param>
public sealed record ProjectRandomValues(string JwtSecret, int ApiHttpPort, int ApiHttpsPort, int UiHttpPort, int UiHttpsPort)
{
    public static ProjectRandomValues Create()
    {
        var apiHttpPort = RandomNumberGenerator.GetInt32(5000, 5300);
        var apiHttpsPort = RandomNumberGenerator.GetInt32(7000, 7300);
        var uiHttpPort = RandomNumberGenerator.GetInt32(5300, 5600);
        var uiHttpsPort = RandomNumberGenerator.GetInt32(7300, 7600);
        var jwtSecret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        return new ProjectRandomValues(jwtSecret, apiHttpPort, apiHttpsPort, uiHttpPort, uiHttpsPort);
    }
}
