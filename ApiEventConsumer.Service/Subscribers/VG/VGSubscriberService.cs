using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Service.Subscribers;

namespace ApiEventConsumer.Subscribers.VG
{
    public interface IVGSubscriberService : ISubscriberService
    {
    }

    public class VGSubcriberService : SubscriberService, IVGSubscriberService
    {
        private const string BaseCode = "USD";

        public VGSubcriberService(
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
            return "AppSettings:SubcriberSettings:VG:StreamSettings";
        }
    }
}