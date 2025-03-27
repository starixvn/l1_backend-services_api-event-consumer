namespace ApiEventConsumer.Subscribers.SC
{
    public interface ISCGameConfigDataAccess : IGameConfigDataAccess
    {
    }

    public class SCGameConfigDataAccess : GameConfigDataAccess, ISCGameConfigDataAccess
    {
        private const string GameConfigCollectionName = "game_configs";

        public SCGameConfigDataAccess(ISCMongoClient mongoClient)
        {
            var database = mongoClient.GetDatabase();

            gameConfigCollection = database.GetCollection<GameConfig>(GameConfigCollectionName);
        }
    }
}