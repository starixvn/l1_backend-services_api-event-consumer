using ApiEventConsumer.Database.Attributes;
using MongoDB.Bson.Serialization.Attributes;

namespace ApiEventConsumer.Database.Entities
{
    [BsonCollection("usersessions")]
    public class UserSessionEntity : Document
    {
        [BsonElement("recheckInTime")]
        public DateTime RecheckInTime { get; set; }

        [BsonElement("partnerId")]
        public string? ParnerId { get; set; }

        [BsonElement("browserUserAgent")]
        public string? BrowserUserAgent { get; set; }

        [BsonElement("playerId")]
        public string? PlayerId { get; set; }

        [BsonElement("ip")]
        public string? Ip { get; set; }
    }
}
