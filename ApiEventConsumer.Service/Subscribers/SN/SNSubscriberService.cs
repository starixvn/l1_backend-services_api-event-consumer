using System.Text;
using System.Text.Json;
using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Api._Shared.Helpers;
using ApiEventConsumer.Core.Models.NatMessage;
using ApiEventConsumer.Database;
using ApiEventConsumer.Database.Entities;
using ApiEventConsumer.Database.Entities.Currencies;
using ApiEventConsumer.Database.Entities.ExchangeRates;
using ApiEventConsumer.Service.Service;
using NATS.Client;
using NATS.Client.JetStream;
using Sentry;

namespace ApiEventConsumer.Subscribers.SN
{
    public interface ISNSubscriberService : ISubscriberService
    {
    }

    public class SNSubscriberService : SubscriberService, ISNSubscriberService
    {
        private const string BaseCode = "USD";
        private readonly IAppSettings appSettings;
        public SNSubscriberService(
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
            var settings = appSettings.SubcriberSettings.SN.StreamSettings;
            return $"{settings.Prefix}_{settings.StreamName}";
        }

        protected override StreamSettings GetStreamSettingsPath()
        {
            return appSettings.SubcriberSettings.SN.StreamSettings;
        }

        protected override string GetSystemTypePath()
        {
            return appSettings.SubcriberSettings.SN.SystemType;
        }

        protected override void InitializeNatsConnection(IConnection? _connection, IAsyncSubscription? _betCreatedSub)
        {
            try
            {
                var streamSettings = GetStreamSettingsPath();

                if (!streamSettings.Enable) return;

                var options = ConnectionFactory.GetDefaultOptions();
                options.Url = streamSettings.Host;
                options.DisconnectedEventHandler = (sender, args) =>
                {
                    sentryClient.CaptureException(new Exception($"Disconnected from NATS server: {args.Error}"));
                };

                _connection = new ConnectionFactory().CreateConnection(options);

                if (_connection.State != ConnState.CONNECTED)
                {
                    sentryClient.CaptureException(new Exception($"Failed to connect to NATS server. State: {_connection.State}"));
                    return;
                }
                var streamName = GetStreamNamePath();
                var jsContext = _connection.CreateJetStreamContext();
                var jsManagement = _connection.CreateJetStreamManagementContext();

                var streamConfig = StreamConfiguration.Builder()
                    .WithName(streamName)
                    .WithSubjects($"{streamName}.*")
                    .WithStorageType(StorageType.Memory)
                    .WithRetentionPolicy(RetentionPolicy.WorkQueue)
                    .Build();

                try
                {
                    var streamInfo = jsManagement.GetStreamInfo(streamName);
                    if (streamInfo == null)
                    {
                        jsManagement.AddStream(streamConfig);
                    }
                }
                catch (NATSJetStreamException ex)
                {
                    sentryClient.CaptureException(ex);
                    return;
                }

                var consumerConfig = ConsumerConfiguration.Builder()
                    .WithAckWait(30000)
                    .WithDeliverPolicy(DeliverPolicy.New)
                    .Build();

                var subscribeOptions = PushSubscribeOptions.Builder()
                    .WithStream(streamName)
                    .WithConfiguration(consumerConfig)
                    .Build();

                _betCreatedSub = jsContext.PushSubscribeAsync($"api.bet.created", HandleBetCreated, false, subscribeOptions);
                _betCreatedSub.Start();
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(new Exception($"Critical error in NATS initialization: {ex}"));
            }
        }

        protected override async void HandleBetCreated(object? sender, MsgHandlerEventArgs args)
        {
            try
            {
                var message = Encoding.UTF8.GetString(args.Message.Data);
                var betCreated = JsonSerializer.Deserialize<BetCreated>(message);
                var systemType = appSettings.SubcriberSettings.SN.SystemType;

                if (betCreated != null)
                {
                    await NotifyHighStake(betCreated, systemType);
                }
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(new Exception($"Error processing bet.created message: {ex}"));
            }
        }
    }
}