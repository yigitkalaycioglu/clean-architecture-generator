//#if Auth
namespace __Name__.Core.Utilities.Security.JWT;

/// <summary>
/// appsettings.json içindeki "TokenOptions" bölümü.
/// </summary>
public sealed class TokenOptions
{
    public const string SectionName = "TokenOptions";

    public string Audience { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    /// <summary>Token geçerlilik süresi (dakika).</summary>
    public int AccessTokenExpiration { get; set; } = 60;

    public string SecurityKey { get; set; } = string.Empty;
}
//#endif
