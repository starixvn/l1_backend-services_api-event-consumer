using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Api._Shared.Helpers;
using ApiEventConsumer.Database;
using ApiEventConsumer.Database.Entities;
using ApiEventConsumer.Database.Entities.Currencies;
using ApiEventConsumer.Database.Entities.ExchangeRates;
using ApiEventConsumer.Service.Service;
using ApiEventConsumer.Service.Subscribers;

namespace ApiEventConsumer.Subscribers.SN
{
    public interface ISNSubscriberService : ISubscriberService
    {
    }

    public class SNSubscriberService : SubscriberService, ISNSubscriberService
    {
        private const string BaseCode = "USD";

        public SNSubscriberService(
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
            return "AppSettings:SubcriberSettings:SN:StreamSettings";
        }


        protected override string GetSystemTypePath()
        {
            return "AppSettings:SubcriberSettings:SN:SystemType";
        }
    }
}