namespace ApiEventConsumer.Subscribers.SN
{
    public interface ISNHighStakeDataAccess : IHighStakeDataAccess
    {
    }

    public class SNHighStakeDataAccess : HighStakeDataAccess, ISNHighStakeDataAccess
    {
        private const string HighStakeCollectionName = "high_stake";

        public SNHighStakeDataAccess(ISNMongoClient mongoClient)
        {
            var database = mongoClient.GetDatabase();

            highStakeCollection = database.GetCollection<ApiEventConsumerLog>(HighStakeCollectionName);
        }
    }
}