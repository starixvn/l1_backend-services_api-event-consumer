namespace ApiEventConsumer.Core.Models.Promotion
{
    public class UpdateTotalTurnoverRequest
    {
        public string PlayerId { get; set; } = string.Empty;
        public string OperatorId { get; set; } = string.Empty;
        public string SiteName { get; set; } = string.Empty;
        public decimal BetAmount { get; set; }
        public decimal GameId { get; set; }

    }
} 