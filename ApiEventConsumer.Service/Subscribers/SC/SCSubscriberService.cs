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
            if (appSettings.SubcriberSettings?.SC?.StreamSettings == null)
            {
                return string.Empty;
            }
            var settings = appSettings.SubcriberSettings.SC.StreamSettings;
            return $"{settings.Prefix}_{settings.StreamName}";
        }

        protected override StreamSettings GetStreamSettingsPath()
        {
            if (appSettings.SubcriberSettings?.SC?.StreamSettings == null)
            {
                return new StreamSettings();
            }
            return appSettings.SubcriberSettings.SC.StreamSettings;
        }

        protected override string GetSystemTypePath()
        {
            if (appSettings.SubcriberSettings?.SC?.SystemType == null)
            {
                return string.Empty;
            }
            return appSettings.SubcriberSettings.SC.SystemType;
        }
    }
}