namespace ApiEventConsumer.Core.Models.Promotion
{
    public class HasBonusWinResponse
    {
        public bool Success { get; set; }
        public bool HasBonusWin { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
} 