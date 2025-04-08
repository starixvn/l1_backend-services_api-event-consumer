using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Core.Enumeration;
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
        private readonly IServiceProvider serviceProvider;
        private readonly IAppSettings appSettings;

        public GeneralGameSettingService(IGenericRepository<GameConfigEntity> repository, IServiceProvider serviceProvider, IAppSettings appSettings)
        {
            this.repository = repository;
            this.serviceProvider = serviceProvider;
            this.appSettings = appSettings;
        }

        public async Task<string> GetDisplayName(int gameId)
        {
            var gameInfo = await GetGameConfig(gameId);

            string displayName;
            if (!string.IsNullOrEmpty(gameInfo.DisplayName))
            {
                displayName = gameInfo.DisplayName;
            }
            else if (!string.IsNullOrEmpty(gameInfo.Name))
            {
                displayName = Game.FromGameName(gameInfo.Name).DisplayName;
            }
            else
            {
                displayName = string.Empty;
            }
            return displayName;
        }

        public async Task<GameConfigEntity> GetGameConfig(int gameId)
        {
            if (appSettings.CloudSettings?.ShouldCallCloudService(gameId) == true)
            {
                return await GetGameConfigThroughGameManagementApi(gameId);
            }

            return await GetGameConfigDirectOnKbtDatabase(gameId);
        }

        private async Task<GameConfigEntity> GetGameConfigThroughGameManagementApi(int gameId)
        {
            var gameManagementApi = serviceProvider.GetRequiredService<IGameManagementApi>();
            var response = await gameManagementApi.GetGameConfigById(gameId);
            return response?.Data ?? new GameConfigEntity { GameId = gameId };
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