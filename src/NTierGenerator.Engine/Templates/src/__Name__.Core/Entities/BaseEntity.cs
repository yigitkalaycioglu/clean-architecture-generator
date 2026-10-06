namespace __Name__.Core.Entities;

/// <summary>
/// Kimlik ve denetim (audit) alanlarını içeren temel varlık.
/// <see cref="CreatedDate"/> ve <see cref="UpdatedDate"/> DbContext tarafından otomatik doldurulur.
/// </summary>
public abstract class BaseEntity : IEntity
{
    public int Id { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }
}
