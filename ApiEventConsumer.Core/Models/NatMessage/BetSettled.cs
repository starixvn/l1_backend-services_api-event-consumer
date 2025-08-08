namespace ApiEventConsumer.Core.Models.NatMessage
{
    public class BetSettled
    {
        public int GameRoundId { get; set; }
        public int GameTypeId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalWin { get; set; }
        public decimal ValidBetAmount { get; set; }
        public DateTimeOffset? EndTime { get; set; }
        public bool? FreeBet { get; set; }
        public string? PromotionId { get; set; } = string.Empty;
        public string Seq { get; set; } = string.Empty;
        public object PlayerId { get; set; } = string.Empty;
        public string SiteId { get; set; } = string.Empty;
        public string OperatorId { get; set; } = string.Empty;
        public string? SiteName { get; set; } = string.Empty;
        public DateTimeOffset TimeStamp { get; }
    }
}
