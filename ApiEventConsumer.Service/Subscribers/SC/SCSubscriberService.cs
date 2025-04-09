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
            if (appSettings.SubcriberSettings?.SC?.StreamConfig?.StreamName == null)
            {
                return string.Empty;
            }
            return appSettings.SubcriberSettings.SC.StreamConfig.StreamName ?? string.Empty;
        }

        protected override StreamConfig GetStreamConfigPath()
        {
            if (appSettings.SubcriberSettings?.SC?.StreamConfig == null)
            {
                return new StreamConfig();
            }
            return appSettings.SubcriberSettings.SC.StreamConfig;
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
            if (appSettings.SubcriberSettings?.SC?.StreamConfig?.BetMessages == null)
            {
                return string.Empty;
            }
            return appSettings.SubcriberSettings.SC.StreamConfig.BetMessages ?? string.Empty;
        }
    }
}