//#if LocalAuth
namespace __Name__.Infrastructure.Identity;

/// <summary>
/// Yenileme token'ı kaydı. Token'ın kendisi değil SHA-256 özeti saklanır; veritabanı ele geçirilse bile kayıtlardan
/// geçerli bir token üretilemez. Aynı girişten türeyen token'lar bir aileyi (FamilyId) paylaşır.
/// </summary>
public sealed class RefreshToken
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid UserId { get; init; }

    /// <summary>Bir girişle başlayıp her yenilemede devam eden oturum zinciri.</summary>
    public Guid FamilyId { get; init; }

    public required string TokenHash { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset ExpiresAt { get; init; }

    /// <summary>Kullanıldığı (yenisiyle değiştirildiği) ya da iptal edildiği an. Dolu olan token bir daha kabul edilmez.</summary>
    public DateTimeOffset? RevokedAt { get; set; }
}
//#endif
