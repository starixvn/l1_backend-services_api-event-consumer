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

    public abstract class SubscriberService : ISubscriberService, IHostedService
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
        private IAsyncSubscription? _betSettledSub;

        private const string HighStakeMessageTemplate = "consumer - {0} - {1} - {2} - {3} - {4} {5}";
        private const double DefaultHighStakeAmount = 2000;
        private const int CacheExpireInMinutes = 60 * 24;

        private readonly IAppSettings appSettings;
        private readonly IGeneralGameSettingService generalGameSettingService;
        private readonly IGameUserService gameUserService;
        private readonly IChatBotApi chatBotApi;


        protected SubscriberService(
            string baseCode, 
            ISentryClient sentryClient,
            IChatBotApi chatBotApi,
            IConfiguration configuration,
            ILogger<SubscriberService> logger,
            IGenericRepository<UserSessionEntity> usersessionRepository,
            IGenericRepository<ExchangeRateEntity> exchangeRateRepository,
            IGenericRepository<CurrencyEntity> currencyRepository,
            IGenericRepository<AccountEntity> userRepository,
            ICacheManager cache,
            IAppSettings appSettings,
            IGameUserService gameUserService,
            IGeneralGameSettingService generalGameSettingService)
        {
            this.baseCode = baseCode;
            this.sentryClient = sentryClient;
            this.chatBotApi = chatBotApi;
            _configuration = configuration;
            _logger = logger;
            this.appSettings = appSettings;
            this.cache = cache;
            this.gameUserService = gameUserService;
            this.usersessionRepository = usersessionRepository;
            this.exchangeRateRepository = exchangeRateRepository;
            this.currencyRepository = currencyRepository;
            this.userRepository = userRepository;
            this.generalGameSettingService = generalGameSettingService;
            uaParser = Parser.GetDefault();
        }

        protected abstract StreamSettings GetStreamSettingsPath(IAppSettings appSettings);
        protected abstract string GetSystemTypePath(IAppSettings appSettings);

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
                var streamSettings = GetStreamSettingsPath(appSettings);

                if (!streamSettings.Enable) return;

                var options = ConnectionFactory.GetDefaultOptions();
                options.Url = streamSettings.Host;
                options.DisconnectedEventHandler = (sender, args) =>
                {
                    // sentryClient.CaptureException(new Exception($"Disconnected from NATS server: {args.Error}"));
                    Console.WriteLine($"[ERROR] Disconnected from NATS server: {args.Error}");
                };

                _connection = new ConnectionFactory().CreateConnection(options);

                if (_connection.State != ConnState.CONNECTED)
                {
                    // sentryClient.CaptureException(new Exception($"Failed to connect to NATS server. State: {_connection.State}"));
                    Console.WriteLine($"[ERROR] Failed to connect to NATS server. State: {_connection.State}");
                    return;
                }

                _betCreatedSub = _connection.SubscribeAsync($"api.bet.created", HandleBetCreated);
                _betCreatedSub.Start();
                Console.WriteLine("[INFO] Successfully subscribed to api.bet.created");
            }
            catch (Exception ex)
            {
                // sentryClient.CaptureException(new Exception($"Critical error in NATS initialization: {ex}"));
                Console.WriteLine($"[ERROR] Critical error in NATS initialization: {ex}");
            }
        }

        public async Task<ChatBotResult> NotifyHighStake(BetCreated bet)
        {
            try
            {
                var highStakeAmount = GetHighStakeAmount();
                var amount = await GetAmount(bet);

                if (bet.Currency == "UUS" || amount < highStakeAmount)
                {
                    return await Task.FromResult<ChatBotResult>(default);
                }

                var msgBuilder = new StringBuilder();
                var game = await GetGameName(bet.GameTypeId);
                var charName = await GetCharName(bet);
                var version = GetVersionName(Enum.Parse<Versions>(bet.System.ToString()), bet.Metadata);

                var playerDeviceExtraInformation = await GetDeviceInformation(bet.PlayerId.ToString());
                msgBuilder.Append(string.Format(HighStakeMessageTemplate, "", charName, bet.Amount, bet.Currency, game, playerDeviceExtraInformation));

                _ = Task.Run(() => chatBotApi.ForwardMessage(
                    appSettings.ChatBotApiSettings.Token,
                    appSettings.ChatBotApiSettings.ConversationIds.HighStake,
                    msgBuilder.ToString()));

                return await Task.FromResult(new ChatBotResult());
            }
            catch (Exception e)
            {
                // sentryClient.CaptureException(e);
                Console.WriteLine($"[ERROR] Error processing high stake notification: {e}");
                return await Task.FromResult(new ChatBotResult());
            }
        }

        public void Dispose()
        {
            try
            {
                _betCreatedSub?.Unsubscribe();
                _betSettledSub?.Unsubscribe();
                _connection?.Dispose();
            }
            catch (Exception ex)
            {
                // sentryClient.CaptureException(new Exception($"Error disposing NATS connections: {ex}"));
                Console.WriteLine($"[ERROR] Error disposing NATS connections: {ex}");
            }
        }

        protected virtual async void HandleBetCreated(object? sender, MsgHandlerEventArgs args)
        {
            try
            {
                var message = Encoding.UTF8.GetString(args.Message.Data);
                var betCreated = JsonSerializer.Deserialize<BetCreated>(message);
                
                if (betCreated != null)
                {
                    Console.WriteLine($"[INFO] Processing bet created: GameRoundId={betCreated.GameRoundId}, Amount={betCreated.Amount}");
                    await NotifyHighStake(betCreated);
                }
            }
            catch (Exception ex)
            {
                // sentryClient.CaptureException(new Exception($"Error processing bet.created message: {ex}"));
                Console.WriteLine($"[ERROR] Error processing bet.created message: {ex}");
            }
        }

        private double GetHighStakeAmount()
        {
            return appSettings.CloudSettings.IsCloudService ? DefaultHighStakeAmount : appSettings.ChatBotApiSettings.HighStakeAmount;
        }

        private async Task<double> GetAmount(BetCreated bet)
        {
            return appSettings.CloudSettings.IsCloudService ? (double)bet.Amount : await CalAmountOnBaseCurrency(bet);
        }

        private async Task<double> CalAmountOnBaseCurrency(BetCreated bet)
        {
            try
            {
                var exchangeRate = GetExchangeRate();
                var currencies = GetCurrencies();
                var user = await GetUser(bet.PlayerId.ToString(), bet.OperatorId);
                var currency = currencies.Where(c => c.Enabled == true && c.Name == bet.Currency && c.PartnerId == user.OperatorId).FirstOrDefault();

                if (currency == null)
                {
                    return (double)bet.Amount;
                }

                var rate = (double)exchangeRate.Code[currency.Code] * (double)currency.Multiplier;
                return (double)bet.Amount * rate;
            }
            catch (Exception e)
            {
                // sentryClient.CaptureException(e);
                Console.WriteLine($"[ERROR] Error calculating amount on base currency: {e}");
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
            return appSettings.CloudSettings.IsCloudService ?
               $"playerId: {bet.PlayerId}" : await gameUserService.GetCharacterNameAsync(bet.PlayerId.ToString());
        }

        private string GetVersionName(Versions version, Dictionary<string, string> metadata)
        {
            var systemType = _configuration.GetSection(GetSystemTypePath(appSettings)).ToString();
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
            if (metadata != null && metadata.ContainsKey("mode") && metadata["mode"] == ClientDisplayMode.Portrait.Value)
            {
                return $" - {ClientDisplayMode.Portrait.DisplayName}";
            }

            return string.Empty;
        }

        private string GetMegaKenoVersionSuffix(string systemType)
        {
            return systemType switch
            {
                SystemType.SC1 => "MK",
                SystemType.VG2 => "VGK",
                _ => string.Empty
            };
        }

        private string GetCocosVersionSuffix(string systemType)
        {
            return systemType switch
            {
                SystemType.SC1 => "C",
                _ => string.Empty
            };
        }

        private string GetSystemPrefixName(string systemType)
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
                if (appSettings.CloudSettings.IsCloudService)
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

                Console.WriteLine($"[INFO] Retrieved device info for player {playerId}");
                return $"- {uaParser.Parse(userSession?.BrowserUserAgent).OS} - {userSession?.Ip}";
            }
            catch (Exception ex)
            {
                // sentryClient.CaptureMessage($"{ex} - {playerId}", SentryLevel.Warning);
                Console.WriteLine($"[WARN] Error getting device information for {playerId}: {ex}");
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
                user = await userRepository.FindOneAsync(u => u.PlayerId == ObCustId && u.OperatorId == operatorId);
                cache.SetCache(userKey, user, CacheExpireInMinutes);
            }

            return user;
        }
    }
}