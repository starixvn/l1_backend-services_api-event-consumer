using NATS.Client;
using System.Text;
using ApiEventConsumer._Shared.Configurations;
using System.Text.Json;
using ApiEventConsumer.Service.Subscribers;
using Refit;
using ApiEventConsumer.Database.Entities.ExchangeRates;
using ApiEventConsumer.Api._Shared.Helpers;
using ApiEventConsumer.Database;
using ApiEventConsumer.Database.Entities.Currencies;
using ApiEventConsumer.Database.Entities;
using ApiEventConsumer.Core.Enumeration;
using ApiEventConsumer.Service.Service;
using UAParser;
using MongoDB.Driver;
using ApiEventConsumer.Core.Models.ChatBot;
using ApiEventConsumer.Core.Models.NatMessage;
using Sentry.Protocol;
using Sentry;
using NATS.Client.JetStream;
using System.Globalization;
using Polly;

namespace ApiEventConsumer.Subscribers
{
    public interface IChatBotApi
    {
        [Post("/forwardwithtoken")]
        Task<ChatBotResult> ForwardMessage(string token, string conversationId, string message);
    }

    public interface ISubscriberService
    {
        void InitializeNatsConnection();
    }

    public class SubscriberServices : ISubscriberService, IHostedService, IDisposable
    {
        protected readonly ISentryClient sentryClient;
        protected readonly IConfiguration configuration;
        private readonly ICacheManager cache;
        private readonly IGenericRepository<UserSessionEntity> usersessionRepository;
        private readonly IGenericRepository<ExchangeRateEntity> exchangeRateRepository;
        private readonly IGenericRepository<CurrencyEntity> currencyRepository;
        private readonly IGenericRepository<AccountEntity> userRepository;
        private readonly Parser uaParser;

        private IConnection? connection;
        private IAsyncSubscription? betCreatedSub;

        private const string HighStakeMessageTemplate = "{0} - {1} - {2} - {3} - {4} {5}";
        private const double DefaultHighStakeAmount = 2000;
        private const int CacheExpireInMinutes = 60 * 24;

        private readonly IAppSettings appSettings;
        private readonly IGeneralGameSettingService generalGameSettingService;
        private readonly IGameUserService gameUserService;
        private readonly IChatBotApi chatBotApi;

        private bool disposed;


        public class ServiceDependencies
        {
            public IGameUserService GameUserService { get; }
            public IGeneralGameSettingService GeneralGameSettingService { get; }
            public IChatBotApi ChatBotApi { get; }

            public ServiceDependencies(
                IGameUserService gameUserService,
                IGeneralGameSettingService generalGameSettingService,
                IChatBotApi chatBotApi)
            {
                GameUserService = gameUserService;
                GeneralGameSettingService = generalGameSettingService;
                ChatBotApi = chatBotApi;
            }
        }

        protected SubscriberServices(
            ISentryClient sentryClient,
            ConfigurationServices configurations,
            RepositoryServices repositories)
        {
            this.sentryClient = sentryClient;
            configuration = configurations.Configuration;
            this.appSettings = configurations.AppSettings;
            this.cache = configurations.Cache;
            
            this.usersessionRepository = repositories.UserSessionRepository;
            this.exchangeRateRepository = repositories.ExchangeRateRepository;
            this.currencyRepository = repositories.CurrencyRepository;
            this.userRepository = repositories.UserRepository;
            
            this.gameUserService = configurations.GameUserService;
            this.generalGameSettingService = configurations.GeneralGameSettingService;
            this.chatBotApi = configurations.ChatBotApi;
            
            uaParser = Parser.GetDefault();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            InitializeNatsConnection();
            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            Dispose();
            await Task.CompletedTask;
        }

        public void InitializeNatsConnection()
        {
            try
            {
                var streamSettings = appSettings.StreamSettings;
                if (streamSettings?.Enable != true)
                {
                    return;
                }

                ConnectToNats(streamSettings);
                ConfigureJetStream();
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(new Exception($"Critical error in NATS initialization: {ex}"));
            }
        }

        private void ConnectToNats(StreamSettings streamSettings)
        {
            var options = ConnectionFactory.GetDefaultOptions();
            options.Url = streamSettings.Host;
            
            ConfigureDisconnectHandler(options);
            
            connection = new ConnectionFactory().CreateConnection(options);
            
            if (connection.State != ConnState.CONNECTED)
            {
                throw new InvalidOperationException($"Failed to connect to NATS server. State: {connection.State}");
            }
        }

        private void ConfigureDisconnectHandler(Options options)
        {
            options.DisconnectedEventHandler = async (_, args) =>
            {
                sentryClient.CaptureException(new Exception($"Disconnected from NATS server: {args.Error}"));
                sentryClient.CaptureMessage("NATS connection lost. Attempting to reconnect...");
                await TryReconnectAsync(options);
            };
        }

        private async Task TryReconnectAsync(Options options)
        {
            var policy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    retryCount: 5,
                    sleepDurationProvider: retryAttempt => 
                        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        sentryClient.CaptureException(
                            new Exception($"Reconnection attempt {retryCount} failed"));
                    });

            try 
            {
                await policy.ExecuteAsync(async () =>
                {
                    if (await Task.FromResult(AttemptReconnectionAsync(options)))
                    {
                        ConfigureJetStream();
                        return;
                    }
                    throw new Exception("Connection attempt failed");
                });
            }
            catch (Exception)
            {
                sentryClient.CaptureException(
                    new Exception("Failed to reconnect to NATS server after maximum retries"));
            }
        }

        private bool AttemptReconnectionAsync(Options options)
        {
            connection = new ConnectionFactory().CreateConnection(options);
            
            if (connection.State == ConnState.CONNECTED)
            {
                sentryClient.CaptureMessage("Successfully reconnected to NATS server");
                return true;
            }
            
            return false;
        }

        private void ConfigureJetStream()
        {
            var streamName = appSettings?.StreamSettings?.StreamName;
            var betMessages = appSettings?.StreamSettings?.BetMessages;
            if (connection == null || streamName == null || betMessages == null)
            {
                return;
            }
            
            var jsContext = connection.CreateJetStreamContext();
            var jsManagement = connection.CreateJetStreamManagementContext();

            ConfigureStream(jsManagement, streamName);
            ConfigureSubscription(jsContext, streamName, betMessages);
        }

        public void ConfigureStream(IJetStreamManagement jsManagement, string streamName)
        {
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
            }
        }

        private void ConfigureSubscription(IJetStream jsContext, string streamName, string betMessages)
        {
            var consumerConfig = ConsumerConfiguration.Builder()
                .WithAckWait(30000)
                .WithDeliverPolicy(DeliverPolicy.New)
                .Build();

            var subscribeOptions = PushSubscribeOptions.Builder()
                .WithStream(streamName)
                .WithConfiguration(consumerConfig)
                .Build();

            betCreatedSub = jsContext.PushSubscribeAsync(betMessages, HandleBetCreated, true, subscribeOptions);
            betCreatedSub.Start();
        }

        public async void HandleBetCreated(object? sender, MsgHandlerEventArgs args)
        {
            try
            {
                var message = Encoding.UTF8.GetString(args.Message.Data);
                var betCreated = JsonSerializer.Deserialize<BetCreated>(message);
                var systemType = appSettings.SystemType ?? string.Empty;

                if (betCreated != null && (double)betCreated.Amount >= appSettings.HighStake)
                {
                    await NotifyHighStake(betCreated, systemType);
                }
            }
            catch (Exception ex)
            {
                sentryClient.CaptureException(new Exception($"Error processing bet.created message: {ex}"));
            }
        }


        public async Task<ChatBotResult> NotifyHighStake(BetCreated bet, string systemType)
        {
            try
            {
                if (!await ShouldNotifyHighStake(bet))
                {
                    return await Task.FromResult(new ChatBotResult());
                }

                var message = await BuildHighStakeMessage(bet, systemType);
                await SendChatBotMessage(message);

                return await Task.FromResult(new ChatBotResult());
            }
            catch (Exception e)
            {
                sentryClient.CaptureException(e);
                return await Task.FromResult(new ChatBotResult());
            }
        }

        private async Task<bool> ShouldNotifyHighStake(BetCreated bet)
        {
            var highStakeAmount = GetHighStakeAmount();
            var amount = await GetAmount(bet);
            return bet.Currency != "UUS" && amount >= highStakeAmount;
        }

        private async Task<string> BuildHighStakeMessage(BetCreated bet, string systemType)
        {
            var game = await GetGameName(bet.GameTypeId);
            var charName = await GetCharName(bet);
            var version = GetVersionName(Enum.Parse<Versions>(bet.System.ToString()), bet.Metadata ?? [], systemType);
            var deviceInfo = await GetDeviceInformation(bet.PlayerId?.ToString() ?? string.Empty);
            var formattedAmount = bet.Amount % 1 == 0 ? ((int)bet.Amount).ToString() : bet.Amount.ToString(CultureInfo.InvariantCulture);

            return string.Format(HighStakeMessageTemplate, version, charName, formattedAmount, bet.Currency, game, deviceInfo);
        }

        private Task<ChatBotResult> SendChatBotMessage(string message)
        {
            return chatBotApi.ForwardMessage(
                appSettings.ChatBotApiSettings?.Token ?? string.Empty,
                appSettings.ChatBotApiSettings?.ConversationIds?.HighStake ?? string.Empty,
                message);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                try
                {
                    betCreatedSub?.Unsubscribe();
                    connection?.Dispose();
                }
                catch (Exception ex)
                {
                    sentryClient.CaptureException(new Exception($"Error disposing NATS connections: {ex}"));
                }
            }

            disposed = true;
        }

        public double GetHighStakeAmount()
        {
            return appSettings.ChatBotApiSettings?.HighStakeAmount ?? DefaultHighStakeAmount;
        }

        public async Task<double> GetAmount(BetCreated bet)
        {
            try
            {
                var (currency, exchangeRate) = await GetCurrencyAndExchangeRate(bet);
                return CalculateAmount(bet.Amount, currency, exchangeRate);
            }
            catch (Exception e)
            {
                sentryClient.CaptureException(e);
                return (double)bet.Amount;
            }
        }

        private async Task<(CurrencyEntity? currency, ExchangeRateEntity exchangeRate)> GetCurrencyAndExchangeRate(BetCreated bet)
        {
            var exchangeRate = GetExchangeRate();
            var currencies = GetCurrencies();
            var user = await GetUser(bet.PlayerId?.ToString() ?? string.Empty, bet.OperatorId ?? string.Empty);
            
            var currency = currencies?.FirstOrDefault(c => 
                c.Enabled && 
                c.Name == bet.Currency && 
                c.PartnerId == user?.OperatorId);

            return (currency, exchangeRate);
        }

        private static double CalculateAmount(decimal betAmount, CurrencyEntity? currency, ExchangeRateEntity exchangeRate)
        {
            if (currency == null || exchangeRate.Code == null || currency.Code == null)
            {
                return (double)betAmount;
            }

            var rate = (decimal)exchangeRate.Code[currency.Code] * (decimal)currency.Multiplier;
            return (double)betAmount * (double)rate;
        }

        public async Task<string> GetGameName(int gameId)
        {
            return gameId switch
            {
                (int)GameId.KenoMatch1 => "KenoSoccer",
                (int)GameId.KenoMatch2 => "KenoSoccer2",
                (int)GameId.KenoPower1 => "KenoPower",
                _ => await generalGameSettingService.GetDisplayName(gameId),
            };
        }

        private async Task<string> GetCharName(BetCreated bet)
        {
            return await gameUserService.GetCharacterNameAsync(bet.PlayerId?.ToString() ?? string.Empty);
        }

        public static string GetVersionName(Versions version, Dictionary<string, string> metadata, string systemType)
        {
            var systemPrefix = GetSystemPrefixName(systemType);

            return version switch
            {
                Versions.Mobi => $"{systemPrefix}M",
                Versions.Instant => $"{systemPrefix}I{GetModeName(metadata)}",
                Versions.Cocos => $"{systemPrefix}{GetCocosVersionSuffix(systemType)}{GetModeName(metadata)}",
                Versions.MegaKeno => $"{GetMegaKenoVersionSuffix(systemType)}{GetModeName(metadata)}",
                Versions.App => $"{systemPrefix}A",
                Versions.Livepage => $"{systemPrefix}L",
                Versions.MobileDirect => $"{systemPrefix}D",
                _ => string.Empty,
            };
        }

        private static string GetModeName(Dictionary<string, string> metadata)
        {
            if (metadata != null && metadata.TryGetValue("mode", out var mode) && mode == ClientDisplayMode.Portrait.Value)
            {
                return $" - {ClientDisplayMode.Portrait.DisplayName}";
            }

            return string.Empty;
        }

        private static string GetMegaKenoVersionSuffix(string systemType)
        {
            if (systemType == SystemType.SC1)
            {
                return "MK";
            }
            if (systemType == SystemType.VG2)
            {
                return "VGK";
            }
            return string.Empty;
        }

        private static string GetCocosVersionSuffix(string systemType)
        {
            return systemType == SystemType.SC1 ? "C" 
                 : string.Empty;
        }

        public static string GetSystemPrefixName(string systemType)
        {
            return systemType switch
            {
                SystemType.SC1 => "S",
                SystemType.VG2 => "VG",
                SystemType.GN => "GN",
                _ => string.Empty
            };
        }

        public async Task<string> GetDeviceInformation(string playerId)
        {
            try
            {
                var projectionUserSession = Builders<UserSessionEntity>.Projection
                     .Include(x => x.PlayerId)
                     .Include(x => x.Ip)
                     .Include(x => x.BrowserUserAgent)
                     .Include(x => x.RecheckInTime);

                var userSession = await usersessionRepository.Collection()
                    .Find(x => x.PlayerId == playerId)
                    .Project<UserSessionEntity>(projectionUserSession)
                    .SortByDescending(x => x.RecheckInTime)
                    .FirstOrDefaultAsync();

                return $"- {uaParser.Parse(userSession?.BrowserUserAgent).OS} - {userSession?.Ip}";
            }
            catch (Exception ex)
            {
                sentryClient.CaptureMessage($"{ex} - {playerId}", SentryLevel.Warning);
                return "- Unknown";
            }
        }

        public ExchangeRateEntity GetExchangeRate()
        {
            var exchangeRate = cache.GetCache<ExchangeRateEntity>(CachedKeys.ExchangeRate.ToString());
            if (exchangeRate == default)
            {
                var projection = Builders<ExchangeRateEntity>.Projection
                    .Include(x => x.ExId)
                    .Include(x => x.Code);

                exchangeRate = this.exchangeRateRepository.Collection()
                    .Find(Builders<ExchangeRateEntity>.Filter.Empty)
                    .Project<ExchangeRateEntity>(projection)
                    .SortByDescending(x => x.ExId)
                    .FirstOrDefault() ?? new ExchangeRateEntity();

                if (exchangeRate.ExId != 0) 
                {
                    cache.SetCache(CachedKeys.ExchangeRate.ToString(), exchangeRate, CacheExpireInMinutes);
                }
            }

            return exchangeRate;
        }

        public IEnumerable<CurrencyEntity> GetCurrencies()
        {
            var currencies = cache.GetCache<IEnumerable<CurrencyEntity>>(CachedKeys.Currencies.ToString());
            if (currencies == default)
            {
                currencies = currencyRepository.AsQueryable();
                cache.SetCache(CachedKeys.Currencies.ToString(), currencies, CacheExpireInMinutes);
            }

            return currencies;
        }

        public async Task<AccountEntity> GetUser(string ObCustId, string operatorId)
        {
            var userKey = string.Format("{0}{1}", CachedKeys.User, ObCustId);
            var user = cache.GetCache<AccountEntity>(userKey);
            if (user == default)
            {
                var projectionUser = Builders<AccountEntity>.Projection
                    .Include(x => x.PlayerId)
                    .Include(x => x.OperatorId);

                user = await userRepository.Collection()
                    .Find(u => u.PlayerId == ObCustId && u.OperatorId == operatorId)
                    .Project<AccountEntity>(projectionUser)
                    .FirstOrDefaultAsync();
                cache.SetCache(userKey, user, CacheExpireInMinutes);
            }

            return user;
        }
    }
}