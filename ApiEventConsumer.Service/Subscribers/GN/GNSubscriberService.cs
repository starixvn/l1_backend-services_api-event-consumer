using ApiEventConsumer._Shared.Configurations;
using Sentry;

namespace ApiEventConsumer.Subscribers.GN
{
    public interface IGNSubscriberService : ISubscriberService
    {
    }

    public class GNSubscriberService : SubscriberService, IGNSubscriberService
    {
        private const string BaseCode = "USD";
        private readonly IAppSettings appSettings;
        public GNSubscriberService(
            ISentryClient sentryClient,
            ILogger<SubscriberService> _logger,
            SubscriberService.ConfigurationDependencies configurations,
            SubscriberService.RepositoryDependencies repositories,
            SubscriberService.ServiceDependencies services)
            : base(BaseCode,
                  sentryClient,
                  _logger,
                  configurations,
                  repositories,
                  services)
        {
            this.appSettings = configurations.AppSettings;
        }

        protected override string GetBetMessages()
        {
            if (appSettings.SubcriberSettings?.GN?.BetMessages == null)
            {
                return string.Empty;
            }
            return appSettings.SubcriberSettings.GN.BetMessages;
        }

        protected override string GetStreamNamePath()
        {
            if (appSettings.SubcriberSettings?.GN?.StreamName == null)
            {
                return string.Empty;
            }
            return appSettings.SubcriberSettings.GN.StreamName;
        }

        protected override StreamSettings GetStreamSettingsPath()
        {
            if (appSettings.StreamSettings == null)
            {
                return new StreamSettings();
            }
            return appSettings.StreamSettings;
        }

        protected override string GetSystemTypePath()
        {
            if (appSettings.SubcriberSettings?.GN?.SystemType == null)
            {
                return string.Empty;
            }
            return appSettings.SubcriberSettings.GN.SystemType;
        }
    }
}