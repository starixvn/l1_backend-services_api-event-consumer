using System.Globalization;
using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Api._Shared.Helpers;
using ApiEventConsumer.Core.Enumeration;
using ApiEventConsumer.Core.Models.ChatBot;
using ApiEventConsumer.Core.Models.NatMessage;
using ApiEventConsumer.Database.Entities.Currencies;
using ApiEventConsumer.Database.Entities.ExchangeRates;
using ApiEventConsumer.Database.Entities;
using ApiEventConsumer.Subscribers;
using MongoDB.Driver;
using UAParser;
using ApiEventConsumer.Database;
using ApiEventConsumer.Core.Constants;

namespace ApiEventConsumer.Services
{
    public interface IHighStakeServices
    {
        Task<ChatBotResult> NotifyHighStake(BetCreated bet, string systemType);
    }

    public class HighStakeServices : IHighStakeServices
    {
        private readonly ISentryClient sentryClient;
        private readonly IAppSettings appSettings;
        private readonly ICacheManager cache;
        private readonly IGenericRepository<UserSessionEntity> userSessionRepository;
        private readonly IGenericRepository<ExchangeRateEntity> exchangeRateRepository;
        private readonly IGenericRepository<CurrencyEntity> currencyRepository;
        private readonly IGenericRepository<AccountEntity> userRepository;
        private readonly IGeneralGameSettingService generalGameSettingService;
        private readonly IGameUserService gameUserService;
        private readonly IChatBotApi chatBotApi;
        private readonly Parser uaParser;

        private const string HighStakeMessageTemplate = "{0} - {1} - {2} - {3} - {4} {5}";
        private const double DefaultHighStakeAmount = 2000;
        private const int CacheExpireInMinutes = 60 * 24;

        public HighStakeServices(
            ISentryClient sentryClient,
            ConfigurationServices configurations,
            RepositoryServices repositories)
        {
            this.sentryClient = sentryClient;
            this.appSettings = configurations.AppSettings;
            this.cache = configurations.Cache;

            this.userSessionRepository = repositories.UserSessionRepository;
            this.exchangeRateRepository = repositories.ExchangeRateRepository;
            this.currencyRepository = repositories.CurrencyRepository;
            this.userRepository = repositories.UserRepository;

            this.gameUserService = configurations.GameUserService;
            this.generalGameSettingService = configurations.GeneralGameSettingService;
            this.chatBotApi = configurations.ChatBotApi;

            uaParser = Parser.GetDefault();
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
            return await generalGameSettingService.GetDisplayName(gameId);
        }

        private async Task<string> GetCharName(BetCreated bet)
        {
            return await gameUserService.GetCharacterNameAsync(bet.PlayerId?.ToString() ?? string.Empty);
        }

        public static string GetVersionName(Versions version, Dictionary<string, string> metadata, string systemType)
        {
            var versionSuffix = GetVersionSuffix(version, systemType);
            var modeSuffix = GetModeName(metadata);

            return $"{versionSuffix}{modeSuffix}";
        }

        private static readonly Dictionary<Versions, string> VersionSuffixes = new()
        {
            { Versions.Mobi, "M" },
            { Versions.Instant, "I" },
            { Versions.App, "A" },
            { Versions.Livepage, "L" },
            { Versions.MobileDirect, "D" },
            { Versions.TodayPage, "T" },
            { Versions.LangLive, "LL" },
            { Versions.OriginalGames, "O" }
        };

        private static string GetVersionSuffix(Versions version, string systemType)
        {
            var systemPrefix = GetSystemPrefixName(systemType);

            if (version == Versions.Cocos){
                return $"{systemPrefix}{GetCocosVersionSuffix(systemType)}";
            }
            
            if (version == Versions.MegaKeno){
                return GetMegaKenoVersionSuffix(systemType);   
            }

            return VersionSuffixes.TryGetValue(version, out var suffix) 
                ? $"{systemPrefix}{suffix}" 
                : string.Empty;
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

                var userSession = await userSessionRepository.Collection()
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
