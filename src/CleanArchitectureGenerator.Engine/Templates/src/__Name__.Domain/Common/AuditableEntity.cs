namespace __Name__.Domain.Common;

/// <summary>
/// Oluşturma ve son değişiklik bilgisini tutan varlıklar. Alanlar elle doldurulmaz; kaydetme sırasında
/// Infrastructure katmanındaki AuditableEntityInterceptor, işlemi yapan kullanıcı ve saatle doldurur.
/// </summary>
public abstract class AuditableEntity : Entity
{
    public DateTimeOffset CreatedAt { get; private set; }

    public string? CreatedBy { get; private set; }

    public DateTimeOffset? LastModifiedAt { get; private set; }

    public string? LastModifiedBy { get; private set; }
}
