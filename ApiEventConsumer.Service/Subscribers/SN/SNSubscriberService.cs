using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Service.Subscribers;

namespace ApiEventConsumer.Subscribers.SN
{
    public interface ISNSubscriberService : ISubscriberService
    {
    }

    public class SNSubscriberService : SubscriberService, ISNSubscriberService
    {
        private const string BaseCode = "USD";

        public SNSubscriberService(
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
            return "AppSettings:SubcriberSettings:SN:StreamSettings";
        }
    }
}