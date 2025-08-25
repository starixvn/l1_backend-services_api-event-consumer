namespace ApiEventConsumer.Core.Models.Promotion
{
    public class PromotionProgressResponse
    {
        public bool Success { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
} 