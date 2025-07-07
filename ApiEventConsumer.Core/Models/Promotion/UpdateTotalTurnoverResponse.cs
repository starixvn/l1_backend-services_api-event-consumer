namespace ApiEventConsumer.Core.Models.Promotion
{
    public class UpdateTotalTurnoverResponse
    {
        public bool Success { get; set; }
        public decimal TotalTurnover { get; set; }
        public decimal RemainingTO { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
} 