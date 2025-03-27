namespace ApiEventConsumer.Subscribers.VG
{
    public interface IVGHighStakeDataAccess : IHighStakeDataAccess
    {
    }

    public class VGHighStakeDataAccess : HighStakeDataAccess, IVGHighStakeDataAccess
    {
        private const string HighStakeCollectionName = "high_stake";

        public VGHighStakeDataAccess(IVGMongoClient mongoClient)
        {
            var database = mongoClient.GetDatabase();

            highStakeCollection = database.GetCollection<ApiEventConsumerLog>(HighStakeCollectionName);
        }
    }
}