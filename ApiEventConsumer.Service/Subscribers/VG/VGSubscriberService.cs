using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Api._Shared.Helpers;
using ApiEventConsumer.Database.Entities.Currencies;
using ApiEventConsumer.Database.Entities.ExchangeRates;
using ApiEventConsumer.Database.Entities;
using ApiEventConsumer.Database;
using ApiEventConsumer.Service.Service;
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
            IConfiguration configuration,
            ILogger<SubscriberService> _logger,
            ICacheManager cache,
            IAppSettings appSettings,
            SubscriberService.RepositoryDependencies repositories,
            SubscriberService.ServiceDependencies services)
            : base(BaseCode,
                  sentryClient,
                  configuration,
                  _logger,
                  cache,
                  appSettings,
                  repositories,
                  services)
        {
            this.appSettings = appSettings;
        }

        protected override string GetStreamNamePath()
        {
            if (appSettings.SubcriberSettings?.VG?.StreamSettings == null)
            {
                return string.Empty;
            }
            var settings = appSettings.SubcriberSettings.VG.StreamSettings;
            return $"{settings.Prefix}_{settings.StreamName}";
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