namespace ApiEventConsumer.Service.Subscribers
{
    public class NotifyHighStakeRequest
    {
        public int GameTypeId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string ObCustId { get; set; } = string.Empty;

    }
} 