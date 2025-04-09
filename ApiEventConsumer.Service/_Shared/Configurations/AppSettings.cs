using ApiEventConsumer.Subscribers;
using NATS.Client.JetStream;

namespace ApiEventConsumer._Shared.Configurations
{
    public interface IAppSettings
    {
        public int Port { get; set; }
        public SubscriberSettings? SubcriberSettings { get; set; }
        public CloudSettings? CloudSettings { get; set; }
        public ChatBotApiSettings? ChatBotApiSettings { get; set; }
        public DatabaseSettings? DatabaseSettings { get; set; }
        public StreamSettings? StreamSettings { get; set; }
    }

    public class AppSettings : IAppSettings
    {
        public int Port { get; set; }
        public SubscriberSettings? SubcriberSettings { get; set; }
        public CloudSettings? CloudSettings { get; set; }
        public ChatBotApiSettings? ChatBotApiSettings { get; set; }
        public DatabaseSettings? DatabaseSettings { get; set; }
        public StreamSettings? StreamSettings { get; set; }

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

    public class StreamSettings
    {
        public string? Host { get; set; }

        public string? SubjectPrefix { get; set; }

        public bool Enable { get; set; }

        public int PublishTimeoutMs { get; set; } = 500;

        // Stream configuration settings
        public string StorageType { get; set; } = "Memory";
        public string RetentionPolicy { get; set; } = "WorkQueue";
        public string DiscardPolicy { get; set; } = "Old";
        public int MaxAgeHours { get; set; } = 1;
        public int MaxSizeMB { get; set; } = 100;
        public int DuplicateWindowMinutes { get; set; } = 2;
        public int MaxMessages { get; set; } = 100_000;
        public int Replicas { get; set; } = 1;

        // Move parsing logic into the settings class
        public StorageType GetStorageType() =>
            StorageType?.ToLower() == "file"
                ? NATS.Client.JetStream.StorageType.File
                : NATS.Client.JetStream.StorageType.Memory;

        public RetentionPolicy GetRetentionPolicy() =>
            RetentionPolicy?.ToLower() switch
            {
                "interest" => NATS.Client.JetStream.RetentionPolicy.Interest,
                "limits" => NATS.Client.JetStream.RetentionPolicy.Limits,
                _ => NATS.Client.JetStream.RetentionPolicy.WorkQueue
            };

        public DiscardPolicy GetDiscardPolicy() =>
            DiscardPolicy?.ToLower() == "new"
                ? NATS.Client.JetStream.DiscardPolicy.New
                : NATS.Client.JetStream.DiscardPolicy.Old;
    }
}