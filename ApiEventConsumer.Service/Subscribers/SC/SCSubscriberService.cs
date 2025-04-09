using ApiEventConsumer._Shared.Configurations;
using Sentry;

namespace ApiEventConsumer.Subscribers.SC
{
    public interface ISCSubscriberService : ISubscriberService
    {
    }

    public class SCSubscriberService : SubscriberService, ISCSubscriberService
    {
        private const string BaseCode = "USD";
        private readonly IAppSettings appSettings;

        public SCSubscriberService(
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

        protected override string GetStreamNamePath()
        {
            if (appSettings.SubcriberSettings?.SC?.StreamName == null)
            {
                return string.Empty;
            }
            return appSettings.SubcriberSettings.SC.StreamName;
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
            if (appSettings.SubcriberSettings?.SC?.SystemType == null)
            {
                return string.Empty;
            }
            return appSettings.SubcriberSettings.SC.SystemType;
        }

        protected override string GetBetMessages()
        {
            if (appSettings.SubcriberSettings?.SC?.BetMessages == null)
            {
                return string.Empty;
            }
            return appSettings.SubcriberSettings.SC.BetMessages;
        }
    }
}