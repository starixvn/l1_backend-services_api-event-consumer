using ApiEventConsumer.Api._Shared.Helpers;
using ApiEventConsumer.Database.Entities.Currencies;
using ApiEventConsumer.Database.Entities.ExchangeRates;
using ApiEventConsumer.Database.Entities;
using ApiEventConsumer.Database;
using ApiEventConsumer.Service.Service;
using ApiEventConsumer.Subscribers;
using ApiEventConsumer._Shared.Configurations;

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
            services.AddSingleton<ISubscriberService, SubscriberServices>();
            services.AddSingleton<IHighStakeServices, HighStakeServices>();
            services.AddHostedService<MessageHandlerHostedService>();
        }
    }

    public class MessageHandlerHostedService : IHostedService
    {
        private readonly INatsConnectionService natsConnection;
        private readonly ISubscriberService messageHandler;
        private readonly IAppSettings appSettings;

        public MessageHandlerHostedService(
        INatsConnectionService natsConnection,
        ISubscriberService messageHandler,
            IAppSettings appSettings)
        {
            this.natsConnection = natsConnection;
            this.messageHandler = messageHandler;
            this.appSettings = appSettings;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            natsConnection.InitializeConnection();
            if (appSettings?.StreamSettings?.BetMessages != null)
            {
                natsConnection.ConfigureJetStream(messageHandler.HandleBetCreated, appSettings.StreamSettings.BetMessages);
            }
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            natsConnection.Disconnect();
            return Task.CompletedTask;
        }
    }
}