using ECommerce.Application.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Caching;
//uygulamanın verileri geçici olarak bellekte saklanmasını sağlar

public class MemoryCacheService(IMemoryCache cache, ILogger<MemoryCacheService> logger) : ICacheService
{
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(10); //cache e koyduğum bir veri için varsayılan yaşam süresi 10 dk olsun

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) // cache de bu key ile kayıt var mı varsa getir
    {
        if (cache.TryGetValue(key, out T? value)) //cache de o veri bulunduysa
        {
            logger.LogDebug("Cache HIT: {CacheKey}", key);
            return Task.FromResult(value);
        }

        logger.LogDebug("Cache MISS: {CacheKey}", key); //cache de o veri bulunamadıysa
        return Task.FromResult<T?>(default);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default)//cache e veri eklemek
    {
        cache.Set(key, value, expiration ?? DefaultExpiration);
        logger.LogDebug("Cache SET: {CacheKey}", key);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default) //cache deki veriyi siler
    {
        cache.Remove(key);
        logger.LogDebug("Cache REMOVE: {CacheKey}", key);
        return Task.CompletedTask;
    }

    public async Task<T> GetOrCreateAsync<T>( //cache de varsa getir yoksa oluştur ve cache e koy
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken ct = default)
    {
        if (cache.TryGetValue(key, out T? cached) && cached is not null) //cache de veri var mı
        {
            logger.LogDebug("Cache HIT: {CacheKey}", key);
            return cached;
        }

        logger.LogDebug("Cache MISS: {CacheKey}", key); //cache de veri yoksa 
        var value = await factory(ct);//factory veriyi getirecek/oluşturacak fonksiyon
        cache.Set(key, value, expiration ?? DefaultExpiration);//cache e koy
        return value;
    }
}