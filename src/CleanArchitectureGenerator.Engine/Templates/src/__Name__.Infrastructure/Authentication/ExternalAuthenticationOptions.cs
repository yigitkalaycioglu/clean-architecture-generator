//#if ExternalAuth
namespace __Name__.Infrastructure.Authentication;

/// <summary>
/// Harici kimlik sağlayıcı ("Authentication" bölümü): Microsoft Entra ID, Auth0, Keycloak, Okta, Google… gibi
/// OpenID Connect uyumlu herhangi bir sağlayıcı. API kendisi parola ya da kullanıcı tutmaz; sağlayıcının
/// imzaladığı erişim token'larını doğrular.
/// </summary>
public sealed class ExternalAuthenticationOptions
{
    public const string SectionName = "Authentication";

    /// <summary>Sağlayıcının adresi; imza anahtarları buradaki OpenID Connect keşif belgesinden alınır.</summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>Token'ın bu API için üretildiğini gösteren "aud" değeri, ör. "api://__Name__".</summary>
    public string Audience { get; set; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Authority) && !string.IsNullOrWhiteSpace(Audience);

    public bool HasSecureAuthority =>
        Uri.TryCreate(Authority, UriKind.Absolute, out var authority) && authority.Scheme == Uri.UriSchemeHttps;
}
//#endif
