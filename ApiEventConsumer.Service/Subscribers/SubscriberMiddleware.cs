using ApiEventConsumer.Api._Shared.Helpers;
using ApiEventConsumer.Database.Entities.Currencies;
using ApiEventConsumer.Database.Entities.ExchangeRates;
using ApiEventConsumer.Database.Entities;
using ApiEventConsumer.Database;
using ApiEventConsumer.Service.Service;
using ApiEventConsumer.Subscribers;
using ApiEventConsumer.Subscribers.Bet;

namespace ApiEventConsumer.Service.Subscribers
{
    public static class SubscriberMiddleware
    {
        public static void AddSubscriberServices(this IServiceCollection services, IConfiguration configuration)
        {
            SentrySdk.ConfigureScope(scope =>
            {
                scope.SetTag("service_name", "ApiEventConsumer");
            });
            
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
            services.AddSingleton<INatsConnectionService, NatsConnectionService>();
            services.AddSingleton<IHighStakeServices, HighStakeServices>();
            services.AddHttpClient();
            services.AddSingleton<IPromotionService, PromotionService>();

            // Register all event subscribers
            services.AddSingleton<BetSubscriber>();
            services.AddSingleton<BetSettledSubscriber>();

            // Register all subscribers as IEventSubscriber for polymorphic access
            services.AddSingleton<IEventSubscriber>(provider => provider.GetRequiredService<BetSubscriber>());
            services.AddSingleton<IEventSubscriber>(provider => provider.GetRequiredService<BetSettledSubscriber>());

            // Now build the provider and create the manager
            var serviceProvider = services.BuildServiceProvider();
            var natsService = serviceProvider.GetRequiredService<INatsConnectionService>();
            var betSubscriber = serviceProvider.GetRequiredService<BetSubscriber>();
            var betSettledSubscriber = serviceProvider.GetRequiredService<BetSettledSubscriber>();
            
            var manager = new SubscriberManager(natsService);
            manager.RegisterSubscriber(betSubscriber);
            manager.RegisterSubscriber(betSettledSubscriber);
            manager.RegisterAllSubscribers();

            // Register the configured manager
            services.AddSingleton<ISubscriberService>(manager);
        }
    }

}