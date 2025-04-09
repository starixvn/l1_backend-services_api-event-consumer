using ApiEventConsumer._Shared.Configurations;
using Sentry;

namespace ApiEventConsumer.Subscribers.SN
{
    public interface ISNSubscriberService : ISubscriberService
    {
    }

    public class SNSubscriberService : SubscriberService, ISNSubscriberService
    {
        private const string BaseCode = "USD";
        private readonly IAppSettings appSettings;
        public SNSubscriberService(
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
            if (appSettings.SubcriberSettings?.SN?.BetMessages == null)
            {
                return string.Empty;
            }
            return appSettings.SubcriberSettings.SN.BetMessages;
        }

        protected override string GetStreamNamePath()
        {
            if (appSettings.SubcriberSettings?.SN?.StreamName == null)
            {
                return string.Empty;
            }
            return appSettings.SubcriberSettings.SN.StreamName;
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
            if (appSettings.SubcriberSettings?.SN?.SystemType == null)
            {
                return string.Empty;
            }
            return appSettings.SubcriberSettings.SN.SystemType;
        }
    }
}