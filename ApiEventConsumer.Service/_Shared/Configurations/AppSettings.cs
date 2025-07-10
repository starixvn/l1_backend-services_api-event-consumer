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
        
        // Stream configuration properties
        public string? SubjectPrefix { get; set; }
        public int MaxAgeHours { get; set; } = 24; // Default 24 hours
        public long MaxMessages { get; set; } = 1000000; // Default 1 million messages
    }
}