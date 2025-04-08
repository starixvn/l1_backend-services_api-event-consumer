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

        protected override string GetStreamNamePath()
        {
            if (appSettings.SubcriberSettings?.SN?.StreamSettings == null)
            {
                return string.Empty;
            }
            var settings = appSettings.SubcriberSettings.SN.StreamSettings;
            return $"{settings.StreamName}_{settings.Prefix}";
        }

        protected override StreamSettings GetStreamSettingsPath()
        {
            if (appSettings.SubcriberSettings?.SN?.StreamSettings == null)
            {
                return new StreamSettings();
            }
            return appSettings.SubcriberSettings.SN.StreamSettings;
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