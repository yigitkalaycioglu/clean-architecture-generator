namespace __Name__.Core.CrossCuttingConcerns.Caching;

/// <summary>
/// Cache soyutlaması. Bellek içi cache yerine Redis vb. kullanmak için yalnızca yeni bir uygulama
/// yazıp CoreModule'deki kaydı değiştirmek yeterlidir.
/// </summary>
public interface ICacheManager
{
    bool TryGet(string key, out object? value);

    void Add(string key, object? value, TimeSpan duration);

    void Remove(string key);

    /// <summary>Anahtarı verilen düzenli ifadeye (regex) uyan tüm kayıtları siler.</summary>
    void RemoveByPattern(string pattern);
}
