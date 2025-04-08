using ApiEventConsumer.Subscribers;

namespace ApiEventConsumer._Shared.Configurations
{
    public interface IAppSettings
    {
        public int Port { get; set; }
        public SubscriberSettings? SubcriberSettings { get; set; }
        public CloudSettings? CloudSettings { get; set; }
        public ChatBotApiSettings? ChatBotApiSettings { get; set; }
        public DatabaseSettings? DatabaseSettings { get; set; }
    }

    public class AppSettings : IAppSettings
    {
        public int Port { get; set; }
        public SubscriberSettings? SubcriberSettings { get; set; }
        public CloudSettings? CloudSettings { get; set; }
        public ChatBotApiSettings? ChatBotApiSettings { get; set; }
        public DatabaseSettings? DatabaseSettings { get; set; }

    }

    public class CloudSettings
    {
        public bool IsCloudService { get; set; }

        public bool EnableCloud { get; set; }

        public string? CloudAdminApiUrl { get; set; }

        public string? ProxyUrl { get; set; }

        public string? GameManagementUrl { get; set; }

        public IEnumerable<int>? CloudGameIds { get; set; }

        public bool IsCloudGame(int gameTypeId) => CloudGameIds != null && CloudGameIds.Contains(gameTypeId);

        public bool ShouldCallCloudService(int gameTypeId)
            => !IsCloudService && IsCloudGame(gameTypeId);
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
        public string? Promotion { get; set; }
        public string? PromotionTournament { get; set; }
    }

    public class DatabaseSettings
    {
        public string? ConnectionString { get; set; }
        public string? DatabaseName { get; set; }
        public string? Collection { get; set; }
    }
}