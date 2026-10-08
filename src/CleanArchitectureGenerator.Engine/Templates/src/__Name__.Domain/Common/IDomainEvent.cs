namespace __Name__.Domain.Common;

/// <summary>
/// Alanda gerçekleşmiş bir olay (ör. "iş tamamlandı"). Varlık olayı kaydeder; değişiklikler veritabanına
/// yazılırken Application katmanındaki IDomainEventHandler uygulamalarına iletilir.
/// </summary>
public interface IDomainEvent;
