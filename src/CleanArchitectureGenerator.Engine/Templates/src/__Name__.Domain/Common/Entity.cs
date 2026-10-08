namespace __Name__.Domain.Common;

/// <summary>
/// Kimliği olan tüm varlıkların temeli. Kimlikler zaman sıralı (sürüm 7) GUID'lerdir: veritabanına gitmeden
/// üretilir, ardışık sayılar gibi tahmin edilemez ve indekste sona eklendiği için yazma performansı korunur.
/// </summary>
public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected Entity()
    {
        Id = Guid.CreateVersion7();
    }

    public Guid Id { get; private init; }

    /// <summary>Henüz yayımlanmamış alan olayları. Değişiklikler kaydedilirken yayımlanıp temizlenir.</summary>
    public IReadOnlyList<IDomainEvent> GetDomainEvents() => [.. _domainEvents];

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}
