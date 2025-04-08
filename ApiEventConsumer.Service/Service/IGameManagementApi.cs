using ApiEventConsumer.Database.Entities;
using Refit;

namespace ApiEventConsumer.Service.Service
{
    [Headers("Authorization: Bearer")]
    public interface IGameManagementApi
    {
        [Get("/game-configs/byid")]
        Task<GameConfigResponse> GetGameConfigById([Query] int gameId);
    }

    public class GameConfigResponse
    {
        public GameConfigEntity? Data { get; set; }
    }
}