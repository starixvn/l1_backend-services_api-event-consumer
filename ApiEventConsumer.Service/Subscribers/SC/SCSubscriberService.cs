using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Api._Shared.Helpers;
using ApiEventConsumer.Database.Entities.Currencies;
using ApiEventConsumer.Database.Entities.ExchangeRates;
using ApiEventConsumer.Database.Entities;
using ApiEventConsumer.Database;
using ApiEventConsumer.Service.Service;

namespace ApiEventConsumer.Subscribers.SC
{
    public interface ISCSubscriberService : ISubscriberService
    {
    }

    public class SCSubscriberService : SubscriberService, ISCSubscriberService
    {
        private const string BaseCode = "USD";

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
        }

        protected override StreamSettings GetStreamSettingsPath(IAppSettings appSettings)
        {
            return appSettings.SubcriberSettings.SN.StreamSettings;
        }

        protected override string GetSystemTypePath(IAppSettings appSettings)
        {
            return appSettings.SubcriberSettings.VG.SystemType;
        }
    }
}