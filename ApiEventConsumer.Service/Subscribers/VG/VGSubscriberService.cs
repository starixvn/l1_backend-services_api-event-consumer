using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Api._Shared.Helpers;
using ApiEventConsumer.Database.Entities.Currencies;
using ApiEventConsumer.Database.Entities.ExchangeRates;
using ApiEventConsumer.Database.Entities;
using ApiEventConsumer.Database;
using ApiEventConsumer.Service.Service;
using Sentry;
using NATS.Client;
using ApiEventConsumer.Core.Models.NatMessage;
using System.Text.Json;
using System.Text;
using NATS.Client.JetStream;

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
            var settings = appSettings.SubcriberSettings.VG.StreamSettings;
            return $"{settings.Prefix}_{settings.StreamName}";
        }

        protected override StreamSettings GetStreamSettingsPath()
        {
            return appSettings.SubcriberSettings.VG.StreamSettings;
        }

        protected override string GetSystemTypePath()
        {
            return appSettings.SubcriberSettings.VG.SystemType;
        }
    }
}