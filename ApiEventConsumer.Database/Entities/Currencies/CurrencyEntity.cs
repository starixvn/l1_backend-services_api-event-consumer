using ApiEventConsumer.Database.Attributes;
using MongoDB.Bson;

namespace ApiEventConsumer.Database.Entities.Currencies
{
    [BsonCollection("currencies")]
    public class CurrencyEntity : Document
    {
        public string Name { get; set; }

        public string Code { get; set; }

        public double Multiplier { get; set; }

        public bool Enabled { get; set; }

        public bool IsTest { get; set; }

        public string PartnerId { get; set; }

        public ObjectId GroupId { get; set; }

        public bool IsUncountOnTotal { get; set; }
    }
}
