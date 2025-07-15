namespace ApiEventConsumer.Core.Models.Promotion
{
    public class PromotionProgressRequest
    {
        public string PlayerId { get; set; } = string.Empty;
        public string OperatorId { get; set; } = string.Empty;
        public string SiteName { get; set; } = string.Empty;
        public decimal TotalTO { get; set; }
        public decimal MinTO { get; set; }
        public decimal RemainTO { get; set; }
        public decimal Progress { get; set; }
        public bool CanClaim { get; set; }
        public decimal Win { get; set; }
        public DateTime Expire { get; set; }
    }
} 