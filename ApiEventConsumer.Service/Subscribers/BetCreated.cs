using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Attributes;

namespace ApiEventConsumer.Service.Subscribers
{
    public class BetCreated
    {
        public int GameRoundId { get; set; }
        public int GameTypeId { get; set; }
        public int ChoiceId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public DateTime? RoundEndTime { get; set; }
        public string Ip { get; set; }
        public bool FreeSpin { get; set; }
        public decimal ContributionAmount { get; set; }
        public decimal ContributionRate { get; set; }
        public string OperatorToken { get; set; }
        public bool FreeBet { get; set; }
        public string OperatorId { get; set; }
        public string? UserId { get; set; }
        public string Seq { get; set; }
        public int PlayerId { get; set; }
        public string SiteId { get; set; }
        public DateTime TimeStamp { get; set; }
        //public int System { get; set; }
        //public string Metadata { get; set; } = string.Empty;
        //public string ObCustId { get; set; } = string.Empty;
    }

    public class ExchangeRate
    {
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime Time { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime ExDate { get; set; }

        public int ExId { get; set; }

        public string Source { get; set; }

        public Dictionary<string, decimal> Code { get; set; }
    }

    public class Currency
    {
        public bool Enabled { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public decimal Multiplier { get; set; }
        public string PartnerId { get; set; }
    }

    public class User
    {
        public string OperatorId { get; set; }
    }
}
