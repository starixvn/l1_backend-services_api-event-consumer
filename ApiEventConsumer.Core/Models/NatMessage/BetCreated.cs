using MongoDB.Bson.Serialization.Attributes;

namespace ApiEventConsumer.Core.Models.NatMessage
{
    public class BetCreated
    {
        public int GameRoundId { get; set; }
        public int GameTypeId { get; set; }
        public int ChoiceId { get; set; }
        public decimal Amount { get; set; }
        public string? Currency { get; set; }
        public DateTime? RoundEndTime { get; set; }
        public string? Ip { get; set; }
        public bool FreeSpin { get; set; }
        public decimal ContributionAmount { get; set; }
        public decimal ContributionRate { get; set; }
        public string? OperatorToken { get; set; }
        public bool FreeBet { get; set; }
        public string? OperatorId { get; set; }
        public string? UserId { get; set; }
        public string? Seq { get; set; }
        public int PlayerId { get; set; }
        public string? SiteId { get; set; }
        public DateTime TimeStamp { get; set; }
        public int System { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
    }
}
