using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Api._Shared.Helpers;
using ApiEventConsumer.Database.Entities.Currencies;
using ApiEventConsumer.Database.Entities.ExchangeRates;
using ApiEventConsumer.Database.Entities;
using ApiEventConsumer.Database;
using ApiEventConsumer.Service.Service;
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