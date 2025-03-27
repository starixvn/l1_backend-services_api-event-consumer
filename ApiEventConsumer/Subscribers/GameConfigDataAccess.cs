using MongoDB.Driver;

namespace ApiEventConsumer.Subscribers
{
    public interface IGameConfigDataAccess
    {
        Task UpdateReloadHighStake();
    }

    public class GameConfigDataAccess : IGameConfigDataAccess
    {
        protected IMongoCollection<GameConfig> gameConfigCollection;

        protected GameConfigDataAccess()
        {
        }

        public Task UpdateReloadHighStake()
        {
            var update = Builders<GameConfig>.Update
                    .Set(field => field.HighStakeReload, true);

            return gameConfigCollection.UpdateManyAsync(config => true, update);
        }
    }
}