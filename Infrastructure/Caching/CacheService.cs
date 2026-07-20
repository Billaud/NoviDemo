using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

// Χρησιμοποιεί IDistributedCache: σε dev/testing μπαίνει in-memory
// (AddDistributedMemoryCache), σε production αντικαθίσταται με Redis
// (AddStackExchangeRedisCache) χωρίς να αλλάξει καθόλου αυτή η κλάση.
public sealed class CacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;

    public CacheService(IDistributedCache distributedCache)
    {
        _distributedCache = distributedCache;
    }

    public async Task<T> GetAsync<T>(string key) where T : class
    {
        var data = await _distributedCache.GetStringAsync(key);
        return data == null ? null : JsonSerializer.Deserialize<T>(data);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiration) where T : class
    {
        var data = JsonSerializer.Serialize(value);
        await _distributedCache.SetStringAsync(key, data, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
        });
    }

    public Task RemoveAsync(string key)
    {
        return _distributedCache.RemoveAsync(key);
    }
}
