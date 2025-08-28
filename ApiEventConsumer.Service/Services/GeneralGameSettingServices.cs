using ApiEventConsumer.Database;
using ApiEventConsumer.Database.Entities;
using System.Collections.Concurrent;

namespace ApiEventConsumer.Services
{
    public interface IGeneralGameSettingService
    {
        Task<string> GetDisplayName(int gameId);
    }

    public class GeneralGameSettingServices : IGeneralGameSettingService
    {
        private readonly IGenericRepository<GameConfigEntity> repository;
        private readonly ConcurrentDictionary<int, CacheEntry> displayNameCache = new();
        private readonly TimeSpan cacheExpiration = TimeSpan.FromSeconds(60);

        public GeneralGameSettingServices(IGenericRepository<GameConfigEntity> repository)
        {
            this.repository = repository;
        }

        private class CacheEntry
        {
            public string DisplayName { get; set; } = string.Empty;
            public DateTime ExpirationTime { get; set; }
        }

        public async Task<string> GetDisplayName(int gameId)
        {
            if (TryGetFromCache(gameId, out var cachedDisplayName))
            {
                return cachedDisplayName;
            }

            var gameInfo = await GetGameConfigDirectOnKbtDatabase(gameId);
            var displayName = ExtractDisplayName(gameInfo);
            UpdateCache(gameId, displayName);

            return displayName;
        }

        private bool TryGetFromCache(int gameId, out string displayName)
        {
            displayName = string.Empty;
            
            if (displayNameCache.TryGetValue(gameId, out var cachedEntry) && 
                DateTime.UtcNow < cachedEntry.ExpirationTime)
            {
                displayName = cachedEntry.DisplayName;
                return true;
            }
            
            return false;
        }

        private static string ExtractDisplayName(GameConfigEntity gameInfo)
        {
            if (!string.IsNullOrEmpty(gameInfo.DisplayName))
                return gameInfo.DisplayName;
            
            if (!string.IsNullOrEmpty(gameInfo.Name))
                return gameInfo.Name;
            
            return string.Empty;
        }

        private void UpdateCache(int gameId, string displayName)
        {
            var newCacheEntry = new CacheEntry
            {
                DisplayName = displayName,
                ExpirationTime = DateTime.UtcNow.Add(cacheExpiration)
            };
            displayNameCache.AddOrUpdate(gameId, newCacheEntry, (key, oldValue) => newCacheEntry);
        }

        private async Task<GameConfigEntity> GetGameConfigDirectOnKbtDatabase(int gameId)
        {
            return await Task.Run(() => repository.AsQueryable()
                .Select(g => new GameConfigEntity
                {
                    GameId = g.GameId,
                    Name = g.Name,
                    DisplayName = g.DisplayName
                }).FirstOrDefault(game => game.GameId == gameId)) ?? new GameConfigEntity { GameId = gameId };
        }
    }
}