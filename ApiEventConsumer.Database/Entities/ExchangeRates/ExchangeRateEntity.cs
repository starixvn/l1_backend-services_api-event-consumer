using ApiEventConsumer.Database.Attributes;
using MongoDB.Bson.Serialization.Attributes;

namespace ApiEventConsumer.Database.Entities.ExchangeRates
{
    [BsonCollection("exchange_rates")]
    public class ExchangeRateEntity : Document
    {
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime Time { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime ExDate { get; set; }

        public int ExId { get; set; }

        public string Source { get; set; }

        public Dictionary<string, decimal> Code { get; set; }
    }
}
