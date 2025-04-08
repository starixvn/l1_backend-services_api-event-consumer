using ApiEventConsumer._Shared.Configurations;
using Sentry;

namespace ApiEventConsumer.Subscribers.VG
{
    public interface IVGSubscriberService : ISubscriberService
    {
    }

    public class VGSubcriberService : SubscriberService, IVGSubscriberService
    {
        private const string BaseCode = "USD";
        private readonly IAppSettings appSettings;

        public VGSubcriberService(
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
            if (appSettings.SubcriberSettings?.VG?.StreamSettings == null)
            {
                return string.Empty;
            }
            var settings = appSettings.SubcriberSettings.VG.StreamSettings;
            return $"{settings.StreamName}_{settings.Prefix}";
        }

        protected override StreamSettings GetStreamSettingsPath()
        {
            if (appSettings.SubcriberSettings?.VG?.StreamSettings == null)
            {
                return new StreamSettings();
            }
            return appSettings.SubcriberSettings.VG.StreamSettings;
        }

        protected override string GetSystemTypePath()
        {
            if (appSettings.SubcriberSettings?.VG?.SystemType == null)
            {
                return string.Empty;
            }
            return appSettings.SubcriberSettings.VG.SystemType;
        }
    }
}