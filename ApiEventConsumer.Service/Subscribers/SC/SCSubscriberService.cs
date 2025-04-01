using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Service.Subscribers;

namespace ApiEventConsumer.Subscribers.SC
{
    public interface ISCSubscriberService : ISubscriberService
    {
    }

    public class SCSubscriberService : SubscriberService, ISCSubscriberService
    {
        private const string BaseCode = "USD";

        public SCSubscriberService(
            ISentryClient sentryClient,
            IConfiguration configuration,
            ILogger<SubscriberService> _logger,
            IChatBotApi chatBotApi,
            IAppSettings appSettings)
            : base(BaseCode, sentryClient, configuration, _logger, chatBotApi, appSettings)
        {
        }

        protected override string GetStreamSettingsPath()
        {
            return "AppSettings:SubcriberSettings:SC:StreamSettings";
        }
    }
}