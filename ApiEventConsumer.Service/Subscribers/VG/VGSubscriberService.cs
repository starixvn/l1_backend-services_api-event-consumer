using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Api._Shared.Helpers;
using ApiEventConsumer.Database.Entities.Currencies;
using ApiEventConsumer.Database.Entities.ExchangeRates;
using ApiEventConsumer.Database.Entities;
using ApiEventConsumer.Database;
using ApiEventConsumer.Service.Service;
using ApiEventConsumer.Service.Subscribers;

namespace ApiEventConsumer.Subscribers.VG
{
    public interface IVGSubscriberService : ISubscriberService
    {
    }

    public class VGSubcriberService : SubscriberService, IVGSubscriberService
    {
        private const string BaseCode = "USD";

        public VGSubcriberService(
            ISentryClient sentryClient,
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

        public override Task<ChatBotResult> ForwardMessage(string token, string conversationId, string message)
        {
            throw new NotImplementedException();
        }

        protected override string GetStreamSettingsPath()
        {
            return "AppSettings:SubcriberSettings:VG:StreamSettings";
        }

        protected override string GetSystemTypePath()
        {
            return "AppSettings:SubcriberSettings:VG:SystemType";
        }
    }
}