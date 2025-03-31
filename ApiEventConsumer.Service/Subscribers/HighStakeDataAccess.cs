using MongoDB.Driver;

namespace ApiEventConsumer.Subscribers
{
    public interface IHighStakeDataAccess
    {
        Task Insert(ApiEventConsumerLog ApiEventConsumerLog);

        Task<ApiEventConsumerLog> GetLastHighStake();
    }

    public class HighStakeDataAccess : IHighStakeDataAccess
    {
        protected IMongoCollection<ApiEventConsumerLog> highStakeCollection;

        protected HighStakeDataAccess()
        {
        }

        public Task Insert(ApiEventConsumerLog ApiEventConsumerLog)
        => highStakeCollection.InsertOneAsync(ApiEventConsumerLog);

        public async Task<ApiEventConsumerLog> GetLastHighStake()
        {
            var projection = Builders<ApiEventConsumerLog>.Projection
                .Include(b => b.ExId);

            var options = new FindOptions<ApiEventConsumerLog, ApiEventConsumerLog>
            {
                Limit = 1,
                Sort = Builders<ApiEventConsumerLog>.Sort.Descending(x => x.ExId),
                Projection = projection
            };

            return (await highStakeCollection.FindAsync(x => true, options)).FirstOrDefault();
        }
    }
}