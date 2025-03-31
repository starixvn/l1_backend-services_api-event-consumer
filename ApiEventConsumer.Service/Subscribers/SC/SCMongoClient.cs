using MongoDB.Driver;

namespace ApiEventConsumer.Subscribers.SC
{
    public interface ISCMongoClient
    {
        IMongoDatabase GetDatabase();
    }

    public class SCMongoClient : ISCMongoClient
    {
        private readonly IMongoDatabase database;

        public SCMongoClient(SubcriberDatabase dbSettings)
        {
            var mongoClient = new MongoClient(dbSettings.ConnectionString);
            database = mongoClient.GetDatabase(dbSettings.DatabaseName);
        }

        public IMongoDatabase GetDatabase() => database;
    }
}