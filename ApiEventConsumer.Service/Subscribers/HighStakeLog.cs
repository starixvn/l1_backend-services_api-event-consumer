using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ApiEventConsumer.Subscribers
{
    public record ApiEventConsumerLog
    {
        public ApiEventConsumerLog() 
        {
            Time = DateTimeOffset.UtcNow.DateTime;
        }

        [BsonRepresentation(BsonType.DateTime)]
        public DateTime Time { get; init; }

        [BsonRepresentation(BsonType.DateTime)]
        public DateTime ExDate { get; init; }

        public int ExId { get; init; }

        public string Source { get; init; }

        public Dictionary<string, decimal> Code { get; init; }
    }
}