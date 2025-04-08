using ApiEventConsumer.Database.Attributes;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ApiEventConsumer.Database.Entities.Currencies
{
    [BsonCollection("currencies")]
    public class CurrencyEntity : Document
    {
        [BsonElement("name")]
        public string? Name { get; set; }

        [BsonElement("code")]
        public string? Code { get; set; }

        [BsonElement("multiplier")]
        public double Multiplier { get; set; }

        [BsonElement("enabled")]
        public bool Enabled { get; set; }

        [BsonElement("isTest")]
        public bool IsTest { get; set; }

        [BsonElement("partnerId")]
        public string? PartnerId { get; set; }

        [BsonElement("groupId")]
        public ObjectId GroupId { get; set; }

        [BsonElement("isUncountOnTotal")]
        public bool IsUncountOnTotal { get; set; }
    }
}
