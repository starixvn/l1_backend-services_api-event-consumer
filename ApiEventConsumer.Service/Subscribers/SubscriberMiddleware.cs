using ApiEventConsumer.Api._Shared.Helpers;
using ApiEventConsumer.Database.Entities.Currencies;
using ApiEventConsumer.Database.Entities.ExchangeRates;
using ApiEventConsumer.Database.Entities;
using ApiEventConsumer.Database;
using ApiEventConsumer.Service.Service;
using ApiEventConsumer.Subscribers.SC;
using ApiEventConsumer.Subscribers.SN;
using ApiEventConsumer.Subscribers.VG;

namespace ApiEventConsumer.Subscribers
{
    public static class SubscriberMiddleware
    {
        public static void AddSubcriberServices(this IServiceCollection services, SubscriberSettings settings)
        {
            // Add Memory Cache
            services.AddMemoryCache();

            // Existing service registrations
            services.AddSingleton(settings);
            services.AddHostedService<SCSubscriberService>();
            services.AddHostedService<VGSubcriberService>();
            services.AddHostedService<SNSubscriberService>();

            // Repository registrations
            services.AddSingleton<IGenericRepository<UserSessionEntity>, MongoRepository<UserSessionEntity>>();
            services.AddSingleton<IGenericRepository<ExchangeRateEntity>, MongoRepository<ExchangeRateEntity>>();
            services.AddSingleton<IGenericRepository<CurrencyEntity>, MongoRepository<CurrencyEntity>>();
            services.AddSingleton<IGenericRepository<AccountEntity>, MongoRepository<AccountEntity>>();
            services.AddSingleton<IGenericRepository<AccountInfoEntity>, MongoRepository<AccountInfoEntity>>();
            services.AddSingleton<IGenericRepository<GameConfigEntity>, MongoRepository<GameConfigEntity>>();

            // Other services
            services.AddSingleton<ICacheManager, CacheManager>();
            services.AddSingleton<IGameUserService, GameUserService>();
            services.AddSingleton<IGeneralGameSettingService, GeneralGameSettingService>();
        }
    }
}