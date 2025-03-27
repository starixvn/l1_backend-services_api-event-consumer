namespace ApiEventConsumer.Subscribers.SN
{
    public interface ISNGameConfigDataAccess : IGameConfigDataAccess
    {
    }

    public class SNGameConfigDataAccess : GameConfigDataAccess, ISNGameConfigDataAccess
    {
        private const string GameConfigCollectionName = "game_configs";

        public SNGameConfigDataAccess(ISNMongoClient mongoClient)
        {
            var database = mongoClient.GetDatabase();

            gameConfigCollection = database.GetCollection<GameConfig>(GameConfigCollectionName);
        }
    }
}