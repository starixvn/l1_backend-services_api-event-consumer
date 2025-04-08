using MongoDB.Bson.Serialization.Attributes;

namespace ApiEventConsumer.Database.Entities
{
    public class MiniGameBaseEntity : Document
    {
        protected MiniGameBaseEntity()
        {
        }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime Time { get; set; }

        public int __v { get; set; }
    }
}