using Microsoft.Extensions.Caching.Memory;

namespace ApiEventConsumer.Api._Shared.Helpers
{
    public interface ISharedCacheService
    {
        void Set<T>(string key, T value, TimeSpan expiration);
        T? Get<T>(string key);
        bool TryGet<T>(string key, out T? value);
    }

    public class SharedCacheService : ISharedCacheService
    {
        private static readonly MemoryCache _cache = new(new MemoryCacheOptions());

        public void Set<T>(string key, T value, TimeSpan expiration)
        {
            var options = new MemoryCacheEntryOptions
            {
                SlidingExpiration = expiration
            };
            
            _cache.Set(key, value, options);
        }

        public T? Get<T>(string key)
        {
            return _cache.TryGetValue(key, out var value) ? (T?)value : default(T);
        }

        public bool TryGet<T>(string key, out T? value)
        {
            if (_cache.TryGetValue(key, out var cacheValue))
            {
                value = (T?)cacheValue;
                return true;
            }
            
            value = default(T);
            return false;
        }
    }
}
