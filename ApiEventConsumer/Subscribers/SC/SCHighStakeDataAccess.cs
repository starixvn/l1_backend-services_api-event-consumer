namespace ApiEventConsumer.Subscribers.SC
{
    public interface ISCHighStakeDataAccess : IHighStakeDataAccess
    {
    }

    public class SCHighStakeDataAccess : HighStakeDataAccess, ISCHighStakeDataAccess
    {
        private const string HighStakeCollectionName = "high_stake";

        public SCHighStakeDataAccess(ISCMongoClient mongoClient)
        {
            var database = mongoClient.GetDatabase();

            highStakeCollection = database.GetCollection<ApiEventConsumerLog>(HighStakeCollectionName);
        }
    }
}