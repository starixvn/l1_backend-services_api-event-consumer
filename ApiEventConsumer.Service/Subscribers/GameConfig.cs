using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ApiEventConsumer.Subscribers
{
    public class GameConfig
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        [BsonElement("_id")]
        public ObjectId Id { get; set; }

        public string Name { get; set; }

        public bool HighStakeReload { get; set; }
    }
}