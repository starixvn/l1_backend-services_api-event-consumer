namespace ApiEventConsumer.Core.Models.Promotion
{
    public class HasBonusWinRequest
    {
        public string PlayerId { get; set; } = string.Empty;
        public string OperatorId { get; set; } = string.Empty;
        public string SiteName { get; set; } = string.Empty;
    }
} 