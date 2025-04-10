using ApiEventConsumer.Database;
using ApiEventConsumer.Database.Entities;

namespace ApiEventConsumer.Service.Service
{
    public interface IGeneralGameSettingService
    {
        Task<string> GetDisplayName(int gameId);
    }

    public class GeneralGameSettingService : IGeneralGameSettingService
    {
        private readonly IGenericRepository<GameConfigEntity> repository;

        public GeneralGameSettingService(IGenericRepository<GameConfigEntity> repository)
        {
            this.repository = repository;
        }

        public async Task<string> GetDisplayName(int gameId)
        {
            var gameInfo = await GetGameConfigDirectOnKbtDatabase(gameId);

            string displayName;
            if (!string.IsNullOrEmpty(gameInfo.DisplayName))
            {
                displayName = gameInfo.DisplayName;
            }
            else if (!string.IsNullOrEmpty(gameInfo.Name))
            {
                displayName = gameInfo.Name;
            }
            else
            {
                displayName = string.Empty;
            }
            return displayName;
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