namespace CleanArchitectureGenerator.Engine;

public enum AuthenticationMode
{
    /// <summary>ASP.NET Core Identity ile yerleşik kullanıcı yönetimi; API kendi JWT'sini üretir.</summary>
    Local,

    /// <summary>OpenID Connect uyumlu harici sağlayıcı (Entra ID, Auth0, Keycloak…); API yalnızca token doğrular.</summary>
    External
}
