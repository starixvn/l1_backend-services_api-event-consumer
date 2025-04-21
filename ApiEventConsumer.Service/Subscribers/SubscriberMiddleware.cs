using ApiEventConsumer.Api._Shared.Helpers;
using ApiEventConsumer.Database.Entities.Currencies;
using ApiEventConsumer.Database.Entities.ExchangeRates;
using ApiEventConsumer.Database.Entities;
using ApiEventConsumer.Database;
using ApiEventConsumer.Service.Service;

namespace ApiEventConsumer.Subscribers
{
    public static class SubscriberMiddleware
    {
        public static void AddSubcriberServices(this IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddSingleton<IGenericRepository<UserSessionEntity>, MongoRepository<UserSessionEntity>>();
            services.AddSingleton<IGenericRepository<ExchangeRateEntity>, MongoRepository<ExchangeRateEntity>>();
            services.AddSingleton<IGenericRepository<CurrencyEntity>, MongoRepository<CurrencyEntity>>();
            services.AddSingleton<IGenericRepository<AccountEntity>, MongoRepository<AccountEntity>>();
            services.AddSingleton<IGenericRepository<AccountInfoEntity>, MongoRepository<AccountInfoEntity>>();
            services.AddSingleton<IGenericRepository<GameConfigEntity>, MongoRepository<GameConfigEntity>>();
            services.AddSingleton<ICacheManager, CacheManager>();
            services.AddSingleton<IGameUserService, GameUserServices>();
            services.AddSingleton<IGeneralGameSettingService, GeneralGameSettingServices>();
            services.AddSingleton<RepositoryServices>();
            services.AddSingleton<ConfigurationServices>();
            services.AddSingleton<IConfigurationService>(sp => sp.GetRequiredService<ConfigurationServices>());
            services.AddHostedService<ConsumerServices>();
        }
    }
}