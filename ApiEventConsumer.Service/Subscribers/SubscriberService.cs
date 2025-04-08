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

    public abstract class SubscriberService : ISubscriberService, IHostedService, IDisposable
    {
        protected readonly string baseCode;
        protected readonly ISentryClient sentryClient;
        protected readonly IConfiguration _configuration;
        protected readonly ILogger<SubscriberService> _logger;
        private readonly ICacheManager cache;
        private readonly IGenericRepository<UserSessionEntity> usersessionRepository;
        private readonly IGenericRepository<ExchangeRateEntity> exchangeRateRepository;
        private readonly IGenericRepository<CurrencyEntity> currencyRepository;
        private readonly IGenericRepository<AccountEntity> userRepository;
        private readonly Parser uaParser;

        private IConnection? _connection;
        private IAsyncSubscription? _betCreatedSub;

        private const string HighStakeMessageTemplate = "Event {0} - {1} - {2} - {3} - {4} {5}";
        private const double DefaultHighStakeAmount = 2000;
        private const int CacheExpireInMinutes = 60 * 24;

        private readonly IAppSettings appSettings;
        private readonly IGeneralGameSettingService generalGameSettingService;
        private readonly IGameUserService gameUserService;
        private readonly IChatBotApi chatBotApi;

        private bool _disposed;

        public class RepositoryDependencies
        {
            public IGenericRepository<UserSessionEntity> UserSessionRepository { get; }
            public IGenericRepository<ExchangeRateEntity> ExchangeRateRepository { get; }
            public IGenericRepository<CurrencyEntity> CurrencyRepository { get; }
            public IGenericRepository<AccountEntity> UserRepository { get; }

            public RepositoryDependencies(
                IGenericRepository<UserSessionEntity> userSessionRepository,
                IGenericRepository<ExchangeRateEntity> exchangeRateRepository,
                IGenericRepository<CurrencyEntity> currencyRepository,
                IGenericRepository<AccountEntity> userRepository)
            {
                UserSessionRepository = userSessionRepository;
                ExchangeRateRepository = exchangeRateRepository;
                CurrencyRepository = currencyRepository;
                UserRepository = userRepository;
            }
        }

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

        public class ConfigurationDependencies
        {
            public IConfiguration Configuration { get; }
            public ICacheManager Cache { get; }
            public IAppSettings AppSettings { get; }

            public ConfigurationDependencies(
                IConfiguration configuration,
                ICacheManager cache,
                IAppSettings appSettings)
            {
                Configuration = configuration;
                Cache = cache;
                AppSettings = appSettings;
            }
        }

        protected SubscriberService(
            string baseCode,
            ISentryClient sentryClient,
            ILogger<SubscriberService> logger,
            ConfigurationDependencies configurations,
            RepositoryDependencies repositories,
            ServiceDependencies services)
        {
            this.baseCode = baseCode;
            this.sentryClient = sentryClient;
            _logger = logger;
            _configuration = configurations.Configuration;
            this.appSettings = configurations.AppSettings;
            this.cache = configurations.Cache;
            
            this.usersessionRepository = repositories.UserSessionRepository;
            this.exchangeRateRepository = repositories.ExchangeRateRepository;
            this.currencyRepository = repositories.CurrencyRepository;
            this.userRepository = repositories.UserRepository;
            
            this.gameUserService = services.GameUserService;
            this.generalGameSettingService = services.GeneralGameSettingService;
            this.chatBotApi = services.ChatBotApi;
            
            uaParser = Parser.GetDefault();
        }

        protected abstract StreamSettings GetStreamSettingsPath();
        protected abstract string GetSystemTypePath();
        protected abstract string GetStreamNamePath();

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
                var streamSettings = GetStreamSettingsPath();
                if (!streamSettings.Enable)
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
            options.DisconnectedEventHandler = (_, args) =>
            {
                sentryClient.CaptureException(new Exception($"Disconnected from NATS server: {args.Error}"));
            };

            _connection = new ConnectionFactory().CreateConnection(options);

            if (_connection.State != ConnState.CONNECTED)
            {
                throw new InvalidOperationException($"Failed to connect to NATS server. State: {_connection.State}");
            }
        }

        private void ConfigureJetStream()
        {
            var streamName = GetStreamNamePath();
            if (_connection == null)
            {
                return;
            }
            
            var jsContext = _connection.CreateJetStreamContext();
            var jsManagement = _connection.CreateJetStreamManagementContext();

            ConfigureStream(jsManagement, streamName);
            ConfigureSubscription(jsContext, streamName);
        }

        private void ConfigureStream(IJetStreamManagement jsManagement, string streamName)
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

        private void ConfigureSubscription(IJetStream jsContext, string streamName)
        {
            var consumerConfig = ConsumerConfiguration.Builder()
                .WithAckWait(30000)
                .WithDeliverPolicy(DeliverPolicy.New)
                .Build();

            var subscribeOptions = PushSubscribeOptions.Builder()
                .WithStream(streamName)
                .WithConfiguration(consumerConfig)
                .Build();

            _betCreatedSub = jsContext.PushSubscribeAsync($"api.bet.created", HandleBetCreated, true, subscribeOptions);
            _betCreatedSub.Start();
        }

        private async void HandleBetCreated(object? sender, MsgHandlerEventArgs args)
        {
            try
            {
                var message = Encoding.UTF8.GetString(args.Message.Data);
                var betCreated = JsonSerializer.Deserialize<BetCreated>(message);
                var systemType = GetSystemTypePath();

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


        public async Task<ChatBotResult> NotifyHighStake(BetCreated bet, string systemType)
        {
            try
            {
                var highStakeAmount = GetHighStakeAmount();
                var amount = await GetAmount(bet);

                if (bet.Currency == "UUS" || amount < highStakeAmount)
                {
                    return await Task.FromResult(new ChatBotResult());
                }

                var msgBuilder = new StringBuilder();
                var game = await GetGameName(bet.GameTypeId);
                var charName = await GetCharName(bet);
                var version = GetVersionName(Enum.Parse<Versions>(bet.System.ToString()), bet.Metadata ?? [], systemType);

                var playerDeviceExtraInformation = await GetDeviceInformation(bet.PlayerId.ToString());
                msgBuilder.Append(string.Format(HighStakeMessageTemplate, version, charName, bet.Amount, bet.Currency, game, playerDeviceExtraInformation));

                _ = Task.Run(() => chatBotApi.ForwardMessage(
                    appSettings.ChatBotApiSettings?.Token ?? string.Empty,
                    appSettings.ChatBotApiSettings?.ConversationIds?.HighStake ?? string.Empty,
                    msgBuilder.ToString()));

                return await Task.FromResult(new ChatBotResult());
            }
            catch (Exception e)
            {
                sentryClient.CaptureException(e);
                return await Task.FromResult(new ChatBotResult());
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                try
                {
                    _betCreatedSub?.Unsubscribe();
                    _connection?.Dispose();
                }
                catch (Exception ex)
                {
                    sentryClient.CaptureException(new Exception($"Error disposing NATS connections: {ex}"));
                }
            }

            _disposed = true;
        }

        private double GetHighStakeAmount()
        {
            return appSettings.CloudSettings?.IsCloudService == true ? DefaultHighStakeAmount : 
                appSettings.ChatBotApiSettings?.HighStakeAmount ?? DefaultHighStakeAmount;
        }

        private async Task<double> GetAmount(BetCreated bet)
        {
            return appSettings.CloudSettings?.IsCloudService == true ? (double)bet.Amount : await CalAmountOnBaseCurrency(bet);
        }

        private async Task<double> CalAmountOnBaseCurrency(BetCreated bet)
        {
            try
            {
                var exchangeRate = GetExchangeRate();
                var currencies = GetCurrencies();
                var user = await GetUser(bet.PlayerId.ToString(), bet.OperatorId ?? string.Empty);
                var currency = currencies?.FirstOrDefault(c => c.Enabled && c.Name == bet.Currency && c.PartnerId == user?.OperatorId);

                if (currency == null || exchangeRate.Code == null || currency.Code == null)
                {
                    return (double)bet.Amount;
                }

                var rate = exchangeRate.Code[currency.Code] * (decimal)currency.Multiplier;
                return (double)bet.Amount * (double)rate;
            }
            catch (Exception e)
            {
                sentryClient.CaptureException(e);
                return (double)bet.Amount;
            }
        }

        private async Task<string> GetGameName(int gameId)
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
            return appSettings.CloudSettings?.IsCloudService == true ?
               $"playerId: {bet.PlayerId}" : await gameUserService.GetCharacterNameAsync(bet.PlayerId.ToString());
        }

        private static string GetVersionName(Versions version, Dictionary<string, string> metadata, string systemType)
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

        private static string GetSystemPrefixName(string systemType)
        {
            return systemType switch
            {
                SystemType.SC1 => "S",
                SystemType.VG2 => "VG",
                SystemType.GN => "GN",
                _ => string.Empty
            };
        }

        private async Task<string> GetDeviceInformation(string playerId)
        {
            try
            {
                if (appSettings.CloudSettings?.IsCloudService == true)
                {
                    return string.Empty;
                }

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
                exchangeRate = this.exchangeRateRepository.AsQueryable().OrderByDescending(ex => ex.ExId).First();
                cache.SetCache(CachedKeys.ExchangeRate.ToString(), exchangeRate, CacheExpireInMinutes);
            }

            return exchangeRate;
        }

        private IEnumerable<CurrencyEntity> GetCurrencies()
        {
            var currencies = cache.GetCache<IEnumerable<CurrencyEntity>>(CachedKeys.Currencies.ToString());
            if (currencies == default)
            {
                currencies = currencyRepository.AsQueryable();
                cache.SetCache(CachedKeys.Currencies.ToString(), currencies, CacheExpireInMinutes);
            }

            return currencies;
        }

        private async Task<AccountEntity> GetUser(string ObCustId, string operatorId)
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