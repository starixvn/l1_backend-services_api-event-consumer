using ApiEventConsumer.Database.Attributes;
using MongoDB.Bson.Serialization.Attributes;

namespace ApiEventConsumer.Database.Entities
{
    [BsonCollection("game_configs")]
    public class GameConfigEntity : MiniGameBaseEntity
    {
        [BsonElement("name")]
        public string? Name { get; set; }

        [BsonElement("displayName")]
        public string? DisplayName { get; set; }

        [BsonElement("gameId")]
        public int GameId { get; set; }
    }
}