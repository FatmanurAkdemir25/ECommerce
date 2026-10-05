namespace ECommerce.Application.Abstractions;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);

    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default);

    Task RemoveAsync(string key, CancellationToken ct = default);

    // Cache'te varsa döner, yoksa factory'yi çalıştırıp sonucu cache'ler
    Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken ct = default);
}