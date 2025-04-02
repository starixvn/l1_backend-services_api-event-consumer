using ApiEventConsumer.Subscribers;

namespace ApiEventConsumer._Shared.Configurations
{
    public partial interface IAppSettings
    {
        SubscriberSettings SubcriberSettings { get; set; }
        CloudSettings CloudSettings { get; set; }
        ChatBotApiSettings ChatBotApiSettings { get; set; }
    }

    public partial class AppSettings : IAppSettings
    {
        public SubscriberSettings SubcriberSettings { get; set; }
        public CloudSettings CloudSettings { get; set; }
        public ChatBotApiSettings ChatBotApiSettings { get; set; }
        public DatabaseSettings DatabaseSettings { get; set; }

    }

    public class CloudSettings
    {
        public bool IsCloudService { get; set; }

        public bool EnableCloud { get; set; }

        public string CloudAdminApiUrl { get; set; }

        public string ProxyUrl { get; set; }

        public string GameManagementUrl { get; set; }

        public IEnumerable<int> CloudGameIds { get; set; }

        public bool IsCloudGame(int gameTypeId) => CloudGameIds != null && CloudGameIds.Contains(gameTypeId);

        public bool ShouldCallCloudService(int gameTypeId)
            => !IsCloudService && IsCloudGame(gameTypeId);
    }

    public class ChatBotApiSettings
    {
        public string Token { get; set; }
        public double HighStakeAmount { get; set; }
        public ConversationIds ConversationIds { get; set; }
    }

    public class ConversationIds
    {
        public string HighStake { get; set; }
    }

    public class DatabaseSettings
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
        public string Collection { get; set; }
    }
}