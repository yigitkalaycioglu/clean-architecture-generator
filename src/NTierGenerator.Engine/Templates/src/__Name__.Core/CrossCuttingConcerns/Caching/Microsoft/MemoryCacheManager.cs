using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;

namespace __Name__.Core.CrossCuttingConcerns.Caching.Microsoft;

/// <summary>
/// <see cref="IMemoryCache"/> tabanlı cache. Eklenen anahtarlar ayrıca izlendiği için desenle toplu silme,
/// .NET sürümleri arasında değişen iç alanlara reflection ile erişmeden güvenle yapılır.
/// </summary>
public sealed class MemoryCacheManager(IMemoryCache memoryCache) : ICacheManager
{
    private readonly ConcurrentDictionary<string, byte> _keys = new();

    public bool TryGet(string key, out object? value)
    {
        return memoryCache.TryGetValue(key, out value);
    }

    public void Add(string key, object? value, TimeSpan duration)
    {
        var options = new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = duration };
        options.RegisterPostEvictionCallback(OnEvicted);
        memoryCache.Set(key, value, options);
        _keys.TryAdd(key, 0);
    }

    public void Remove(string key)
    {
        memoryCache.Remove(key);
        _keys.TryRemove(key, out _);
    }

    public void RemoveByPattern(string pattern)
    {
        var regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
        foreach (var key in _keys.Keys.Where(key => regex.IsMatch(key)).ToList())
        {
            Remove(key);
        }
    }

    private void OnEvicted(object key, object? value, EvictionReason reason, object? state)
    {
        // Aynı anahtar yeniden yazıldıysa (Replaced) ya da tekrar eklendiyse takip sürer.
        if (reason != EvictionReason.Replaced && key is string text && !memoryCache.TryGetValue(text, out _))
        {
            _keys.TryRemove(text, out _);
        }
    }
}
