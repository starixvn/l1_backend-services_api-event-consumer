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
    }

    public class CloudSettings
    {
        public bool IsCloudService { get; set; }
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
}