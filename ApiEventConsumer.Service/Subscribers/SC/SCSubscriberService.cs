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
            IChatBotApi chatBotApi,
            IConfiguration configuration,
            ILogger<SubscriberService> _logger,
            IGenericRepository<UserSessionEntity> usersessionRepository,
            IGenericRepository<ExchangeRateEntity> exchangeRateRepository,
            IGenericRepository<CurrencyEntity> currencyRepository,
            IGenericRepository<AccountEntity> userRepository,
            ICacheManager cache,
            IAppSettings appSettings,
            IGameUserService gameUserService,
            IGeneralGameSettingService generalGameSettingService)
            : base(BaseCode,
                  sentryClient,
                  chatBotApi,
                  configuration,
                  _logger,
                  usersessionRepository,
                  exchangeRateRepository,
                  currencyRepository,
                  userRepository,
                  cache,
                  appSettings,
                  gameUserService,
                  generalGameSettingService)
        {
            this.appSettings = appSettings;
        }

        protected override string GetStreamNamePath()
        {
            var settings = appSettings.SubcriberSettings.SC.StreamSettings;
            return $"{settings.Prefix}_{settings.StreamName}";
        }

        protected override StreamSettings GetStreamSettingsPath()
        {
            return appSettings.SubcriberSettings.SC.StreamSettings;
        }

        protected override string GetSystemTypePath()
        {
            return appSettings.SubcriberSettings.SC.SystemType;
        }
    }
}