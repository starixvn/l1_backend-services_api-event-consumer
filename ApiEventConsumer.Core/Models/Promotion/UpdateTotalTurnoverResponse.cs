namespace ApiEventConsumer.Core.Models.Promotion
{
    public class UpdateTotalTurnoverResponse
    {
        public bool Success { get; set; }
        public decimal TotalTurnover { get; set; }
        public decimal MinRequiredTurnover { get; set; }
        public decimal RemainingTO { get; set; }
        public decimal ProgressPercentage { get; set; }
        public bool CanClaim { get; set; }
        public decimal TotalWinAmount { get; set; }
        public DateTime ClaimExpirationDate { get; set; }
        public string ErrorCode { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }
} 