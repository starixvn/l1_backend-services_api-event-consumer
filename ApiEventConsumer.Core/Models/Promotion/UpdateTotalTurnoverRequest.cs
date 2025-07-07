namespace ApiEventConsumer.Core.Models.Promotion
{
    public class UpdateTotalTurnoverRequest
    {
        public string PlayerId { get; set; }
        public string OperatorId { get; set; }
        public string SiteName { get; set; }
        public string PromotionId { get; set; }
        public decimal BetAmount { get; set; }
    }
} 