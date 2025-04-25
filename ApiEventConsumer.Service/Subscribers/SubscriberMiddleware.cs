using ApiEventConsumer.Api._Shared.Helpers;
using ApiEventConsumer.Database.Entities.Currencies;
using ApiEventConsumer.Database.Entities.ExchangeRates;
using ApiEventConsumer.Database.Entities;
using ApiEventConsumer.Database;
using ApiEventConsumer.Service.Service;
using ApiEventConsumer.Subscribers;
using ApiEventConsumer.Subscribers.Bet;
using ApiEventConsumer.Subscribers.User;
using ApiEventConsumer._Shared.Configurations;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace ApiEventConsumer.Service.Subscribers
{
    public static class SubscriberMiddleware
    {
        public static void AddSubcriberServices(this IServiceCollection services)
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

            // Register all event subscribers
            services.AddSingleton<BetSubscriber>();
            services.AddSingleton<UserSubscriber>();

            // Register all subscribers as IEventSubscriber for polymorphic access
            services.AddSingleton<IEventSubscriber>(provider => provider.GetRequiredService<BetSubscriber>());
            services.AddSingleton<IEventSubscriber>(provider => provider.GetRequiredService<UserSubscriber>());

            // Register the subscriber manager (implements ISubscriberService for backward compatibility)
            services.AddSingleton<ISubscriberService>(provider => {
                var natsService = provider.GetRequiredService<INatsConnectionService>();
                var manager = new SubscriberManager(natsService);
                
                // Register all subscribers with the manager
                manager.RegisterSubscriber(provider.GetRequiredService<BetSubscriber>());
                manager.RegisterSubscriber(provider.GetRequiredService<UserSubscriber>());

                // Register all subscribers to NATS
                manager.RegisterAllSubscribers();
                
                return manager;
            });
        }
    }

}