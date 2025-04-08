using Microsoft.Extensions.Caching.Memory;

namespace ApiEventConsumer.Api._Shared.Helpers
{
    public enum CachedKeys
    {
        ExchangeRate,
        Currencies,
        User
    }

    public interface ICacheManager
    {
        void SetCache<T>(string key, T value, int ExpiredInMinutes);

        T? GetCache<T>(string key);
    }

    public class CacheManager : ICacheManager
    {
        private const int DefaultExpiredInMinutes = 2;

        private readonly IMemoryCache cache;

        public CacheManager(IMemoryCache cache)
        {
            this.cache = cache;
        }

        public void SetCache<T>(string key, T value, int ExpiredInMinutes = DefaultExpiredInMinutes)
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                   .SetSlidingExpiration(TimeSpan.FromMinutes(DefaultExpiredInMinutes));

            cache.Set(key, value, cacheEntryOptions);
        }

        public T? GetCache<T>(string key)
        {
            T? value;

            if (cache.TryGetValue(key, out value))
            {
                return value;
            }

            return default;
        }
    }
}
