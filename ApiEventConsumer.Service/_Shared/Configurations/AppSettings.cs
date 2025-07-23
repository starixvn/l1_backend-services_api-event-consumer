namespace ApiEventConsumer._Shared.Configurations
{
    public interface IAppSettings
    {
        public int Port { get; set; }
        public string? SystemType { get; set; }
        public double HighStake { get; set; }
        public PromotionApiSettings? PromotionApiSettings { get; set; }
        public ChatBotApiSettings? ChatBotApiSettings { get; set; }
        public DatabaseSettings? DatabaseSettings { get; set; }
        public StreamSettings? StreamSettings { get; set; }
        public NotificationApiSettings? NotificationApiSettings { get; set; }
        public AuthApiSettings? AuthApiSettings { get; set; }
    }

    public class AppSettings : IAppSettings
    {
        public int Port { get; set; }
        public string? SystemType { get; set; }
        public double HighStake { get; set; }
        public PromotionApiSettings? PromotionApiSettings { get; set; }
        public ChatBotApiSettings? ChatBotApiSettings { get; set; }
        public DatabaseSettings? DatabaseSettings { get; set; }
        public StreamSettings? StreamSettings { get; set; }
        public NotificationApiSettings? NotificationApiSettings { get; set; }
        public AuthApiSettings? AuthApiSettings { get; set; }

    }

    public class ChatBotApiSettings
    {
        public string? Token { get; set; }

        public string? Url { get; set; }

        public ConversationIds? ConversationIds { get; set; }

        public int TimeoutInSeconds { get; set; }

        public string? Proxy { get; set; }

        public double HighStakeAmount { get; set; }
    }

    public class ConversationIds
    {
        public string? HighStake { get; set; }
    }

    public class PromotionApiSettings
    {
        public string? BaseUrl { get; set; }
        public PromotionEndpoints? Endpoints { get; set; }
        public int? TimeoutInSeconds { get; set; }
    }

    public class PromotionEndpoints
    {
        public string UpdateTotalTurnover { get; set; } = "/bonus-wins/update-total-turnover";
        public string HasBonusWin { get; set; } = "/bonus-wins/has-bonus-win";
    }

    public class DatabaseSettings
    {
        public string? ConnectionString { get; set; }
        public string? DatabaseName { get; set; }
        public string? Collection { get; set; }
    }

    public class StreamSettings
    {
        public string? Host { get; set; }

        public bool Enable { get; set; }
        public string? StreamName { get; set; }
        public string? BetMessages { get; set; }
        public string? BetSettledMessages { get; set; }
        
        // Connection configuration properties
        public int MaxReconnectAttempts { get; set; } = 10;
        public int ReconnectWaitMs { get; set; } = 10000;
        public int ShutdownWaitMs { get; set; } = 500;
        
        // Consumer configuration
        public ConsumerSettings? ConsumerSettings { get; set; }
    }

    public class ConsumerSettings
    {
        public int AckWaitMs { get; set; } = 30000;
        public int MaxAckPending { get; set; } = 50;
        public int MaxDeliver { get; set; } = 10;
        public int PullBatchSize { get; set; } = 10;
        public int PullTimeoutMs { get; set; } = 1000;
        public int NoMessageDelayMs { get; set; } = 100;
        public int ErrorRetryDelayMs { get; set; } = 1000;
    }

    public class NotificationApiSettings
    {
        public string? BaseUrl { get; set; }
        public string? PromotionProgressEndpoint { get; set; } = "/api/promotionProgress";
        public int TimeoutInSeconds { get; set; } = 30;
    }

    public class AuthApiSettings
    {
        public string? BaseUrl { get; set; }
        public string? TokenEndpoint { get; set; } = "/api/token";
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? Scope { get; set; }
        public int TimeoutInSeconds { get; set; } = 30;
    }
}