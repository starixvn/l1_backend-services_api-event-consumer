namespace ApiEventConsumer._Shared.Configurations
{
    public interface IAppSettings
    {
        public int Port { get; set; }
        public string? SystemType { get; set; }
        public decimal HighStake { get; set; }
        public PromotionApiSettings? PromotionApiSettings { get; set; }
        public ChatBotApiSettings? ChatBotApiSettings { get; set; }
        public DatabaseSettings DatabaseSettings { get; set; }
        public StreamSettings StreamSettings { get; set; }
        public NotificationApiSettings? NotificationApiSettings { get; set; }
        public AuthApiSettings? AuthApiSettings { get; set; }
    }

    public class AppSettings : IAppSettings
    {
        public int Port { get; set; }
        public string? SystemType { get; set; }
        public decimal HighStake { get; set; }
        public PromotionApiSettings? PromotionApiSettings { get; set; }
        public ChatBotApiSettings? ChatBotApiSettings { get; set; }
        public DatabaseSettings DatabaseSettings { get; set; } = null!;
        public StreamSettings StreamSettings { get; set; } = null!;
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
        public string? HighMultiplier { get; set; }
    }

    public class PromotionApiSettings
    {
        public string? BaseUrl { get; set; }
        public PromotionEndpoints? Endpoints { get; set; }
        public int? TimeoutInSeconds { get; set; }
    }

    public class PromotionEndpoints
    {
        public string UpdateTotalTurnover { get; set; } = null!;
        public string HasBonusWin { get; set; } = null!;
        public string GetRunningPromotions { get; set; } = null!;
    }

    public class DatabaseSettings
    {
        public string ConnectionString { get; set; } = null!;
        public string DatabaseName { get; set; } = null!;
        public string? Collection { get; set; }
    }

    public class StreamSettings
    {
        public string? Host { get; set; }
        public bool Enable { get; set; }
        public string? StreamName { get; set; }
        public string? BetMessages { get; set; }
        public string? BetSettledMessages { get; set; }
        public int MaxReconnectAttempts { get; set; }
        public int ReconnectWaitMs { get; set; }
        public int ShutdownWaitMs { get; set; }
        public ConsumerSettings ConsumerSettings { get; set; } = null!;
    }

    public class ConsumerSettings
    {
        public int AckWaitMs { get; set; }
        public int MaxAckPending { get; set; }
        public int MaxDeliver { get; set; }
        public int PullBatchSize { get; set; }
        public int PullTimeoutMs { get; set; }
        public int NoMessageDelayMs { get; set; }
        public int ErrorRetryDelayMs { get; set; }
    }

    public class NotificationApiSettings
    {
        public string? BaseUrl { get; set; }
        public string? PromotionProgressEndpoint { get; set; }
        public int TimeoutInSeconds { get; set; }
    }

    public class AuthApiSettings
    {
        public string? BaseUrl { get; set; }
        public string? TokenEndpoint { get; set; }
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? Scope { get; set; }
        public int TimeoutInSeconds { get; set; }
    }
}