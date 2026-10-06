//#if Auth
namespace __Name__.Core.Entities.Concrete;

/// <summary>
/// Uygulama kullanıcısı. Kimlik doğrulama altyapısı (JWT) projeden bağımsız olduğu için Core katmanındadır.
/// Parola düz metin olarak değil, PBKDF2 özeti ve salt olarak saklanır.
/// </summary>
public class User : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public byte[] PasswordHash { get; set; } = [];

    public byte[] PasswordSalt { get; set; } = [];

    /// <summary>Pasif kullanıcılar giriş yapamaz.</summary>
    public bool Status { get; set; } = true;
}
//#endif
