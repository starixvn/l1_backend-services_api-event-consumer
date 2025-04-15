using ApiEventConsumer.Database.Attributes;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ApiEventConsumer.Database.Entities.ExchangeRates
{
    [BsonCollection("exchange_rates")]
    public class ExchangeRateEntity : Document
    {
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        [BsonElement("time")]
        public DateTime Time { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        [BsonElement("exDate")]
        public DateTime ExDate { get; set; }

        [BsonElement("exId")]
        public int ExId { get; set; }

        [BsonElement("source")]
        public string? Source { get; set; }

        [BsonElement("code")]
        public Dictionary<string, Decimal128>? Code { get; set; }
    }
}
