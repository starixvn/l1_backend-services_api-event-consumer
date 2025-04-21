using ApiEventConsumer.Api._Shared.Helpers;
using ApiEventConsumer.Database.Entities.Currencies;
using ApiEventConsumer.Database.Entities.ExchangeRates;
using ApiEventConsumer.Database.Entities;
using ApiEventConsumer.Database;
using ApiEventConsumer.Service.Service;
using ApiEventConsumer._Shared.Configurations;

namespace ApiEventConsumer.Subscribers
{
    public static class SubscriberMiddleware
    {
        public static void AddSubcriberServices(this IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddHostedService<ConsumerServices>();
            services.AddSingleton<IGenericRepository<UserSessionEntity>, MongoRepository<UserSessionEntity>>();
            services.AddSingleton<IGenericRepository<ExchangeRateEntity>, MongoRepository<ExchangeRateEntity>>();
            services.AddSingleton<IGenericRepository<CurrencyEntity>, MongoRepository<CurrencyEntity>>();
            services.AddSingleton<IGenericRepository<AccountEntity>, MongoRepository<AccountEntity>>();
            services.AddSingleton<IGenericRepository<AccountInfoEntity>, MongoRepository<AccountInfoEntity>>();
            services.AddSingleton<IGenericRepository<GameConfigEntity>, MongoRepository<GameConfigEntity>>();

            services.AddSingleton<ICacheManager, CacheManager>();
            services.AddSingleton<IGameUserService, GameUserServices>();
            services.AddSingleton<IGeneralGameSettingService, GeneralGameSettingServices>();

            services.AddSingleton<RepositoryServices>(sp => new RepositoryServices(
                sp.GetRequiredService<IGenericRepository<UserSessionEntity>>(),
                sp.GetRequiredService<IGenericRepository<ExchangeRateEntity>>(),
                sp.GetRequiredService<IGenericRepository<CurrencyEntity>>(),
                sp.GetRequiredService<IGenericRepository<AccountEntity>>()
            ));

            services.AddSingleton<ConfigurationServices>(sp => new ConfigurationServices(
                    sp.GetRequiredService<IConfiguration>(),
                    sp.GetRequiredService<ICacheManager>(),
                    sp.GetRequiredService<IAppSettings>(),
                    sp.GetRequiredService<IGameUserService>(),
                    sp.GetRequiredService<IGeneralGameSettingService>(),
                    sp.GetRequiredService<IChatBotApi>()
                ));
        }
    }
}