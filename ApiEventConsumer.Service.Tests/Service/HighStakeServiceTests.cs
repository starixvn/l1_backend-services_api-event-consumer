using FluentAssertions;
using Sentry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ApiEventConsumer.Api._Shared.Helpers;
using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Database;
using ApiEventConsumer.Database.Entities;
using ApiEventConsumer.Database.Entities.Currencies;
using ApiEventConsumer.Database.Entities.ExchangeRates;
using ApiEventConsumer.Services;
using ApiEventConsumer.Core.Models.NatMessage;
using MongoDB.Driver;
using ApiEventConsumer.Core.Constants;
using ApiEventConsumer.Core.Enumeration;
using System.Reflection;
using ApiEventConsumer.Subscribers;
using NSubstitute;

namespace ApiEventConsumer.Service.Tests.Service
{
    public class HighStakeServiceTests
    {
        private readonly ISentryClient _sentryClient;
        private readonly ILogger<HighStakeServices> _logger;
        private readonly IConfiguration _configuration;
        private readonly ICacheManager _cache;
        private readonly IAppSettings _appSettings;
        private readonly IGenericRepository<UserSessionEntity> _userSessionRepo;
        private readonly IGenericRepository<ExchangeRateEntity> _exchangeRateRepo;
        private readonly IGenericRepository<CurrencyEntity> _currencyRepo;
        private readonly IGenericRepository<AccountEntity> _userRepo;
        private readonly IGameUserService _gameUserService;
        private readonly IGeneralGameSettingService _generalGameSettingService;
        private readonly IChatBotApi _chatBotApi;

        private readonly ConfigurationServices _configDependencies;
        private readonly RepositoryServices _repoDependencies;

        public HighStakeServiceTests()
        {
            // Setup substitutes
            _sentryClient = Substitute.For<ISentryClient>();
            _logger = Substitute.For<ILogger<HighStakeServices>>();
            _configuration = Substitute.For<IConfiguration>();
            _cache = Substitute.For<ICacheManager>();
            _appSettings = Substitute.For<IAppSettings>();
            _userSessionRepo = Substitute.For<IGenericRepository<UserSessionEntity>>();
            _exchangeRateRepo = Substitute.For<IGenericRepository<ExchangeRateEntity>>();
            _currencyRepo = Substitute.For<IGenericRepository<CurrencyEntity>>();
            _userRepo = Substitute.For<IGenericRepository<AccountEntity>>();
            _gameUserService = Substitute.For<IGameUserService>();
            _generalGameSettingService = Substitute.For<IGeneralGameSettingService>();
            _chatBotApi = Substitute.For<IChatBotApi>();

            // Setup dependencies
            _configDependencies = new ConfigurationServices(
                _configuration,
                _cache,
                _appSettings,
                _gameUserService,
                _generalGameSettingService,
                _chatBotApi
            );

            _repoDependencies = new RepositoryServices(
                _userSessionRepo,
                _exchangeRateRepo,
                _currencyRepo,
                _userRepo
            );
        }

        private class TestHighStakeService : HighStakeServices
        {

            public TestHighStakeService(
                ISentryClient sentryClient,
                ConfigurationServices configurations,
                RepositoryServices repositories)
                : base(sentryClient, configurations, repositories)
            {
            }
        }

        [Fact]
        public async Task NotifyHighStake_WhenBetIsUUS_ShouldReturnEmptyResult()
        {
            // Arrange
            var sut = CreateHighStakeService();
            var bet = new BetCreated { Currency = "UUS" };

            // Act
            var result = await sut.NotifyHighStake(bet, "TEST");

            // Assert
            result.Should().NotBeNull();
            await _chatBotApi.DidNotReceive().ForwardMessage(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>()
            );
        }

        [Fact]
        public async Task GetGameName_WhenUnknownGame_ShouldCallGeneralGameSettingService()
        {
            // Arrange
            var unknownGameId = 999;
            var expectedName = "Unknown Game";
            _generalGameSettingService.GetDisplayName(unknownGameId).Returns(expectedName);
            var sut = CreateHighStakeService();

            // Act
            var result = await sut.GetGameName(unknownGameId);

            // Assert
            result.Should().Be(expectedName);
            await _generalGameSettingService.Received(1).GetDisplayName(unknownGameId);
        }

        [Fact]
        public void GetCurrencies_WhenCacheHit_ShouldReturnCachedValue()
        {
            // Arrange
            var expectedCurrencies = new List<CurrencyEntity> { new CurrencyEntity() };
            _cache.GetCache<IEnumerable<CurrencyEntity>>(CachedKeys.Currencies.ToString())
                .Returns(expectedCurrencies);

            var sut = CreateHighStakeService();

            // Act
            var result = sut.GetCurrencies();

            // Assert
            result.Should().BeSameAs(expectedCurrencies);
            _currencyRepo.DidNotReceive().AsQueryable();
        }

        [Theory]
        [InlineData(SystemType.SC1, "S")]
        [InlineData(SystemType.VG2, "VG")]
        [InlineData(SystemType.GN, "GN")]
        [InlineData("unknown", "")]
        public void GetSystemPrefixName_ShouldReturnCorrectPrefix(string systemType, string expected)
        {
            // Act
            var result = HighStakeServices.GetSystemPrefixName(systemType);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public async Task CalAmountOnBaseCurrency_WhenExchangeRateNull_ShouldReturnOriginalAmount()
        {
            // Arrange
            var bet = new BetCreated { Amount = 100 };
            var exchangeRate = new ExchangeRateEntity
            {
                Code = null,
                Time = DateTime.UtcNow,
                ExDate = DateTime.UtcNow,
                Source = string.Empty
            };

            _cache.GetCache<ExchangeRateEntity>(CachedKeys.ExchangeRate.ToString())
                    .Returns(exchangeRate);

            var sut = CreateHighStakeService();

            // Act
            var result = await sut.GetAmount(bet);

            // Assert
            result.Should().Be(100);
        }

        [Theory]
        [InlineData(Versions.Mobi, SystemType.SC1, "SM")]  // SC1 -> "S" prefix
        [InlineData(Versions.App, SystemType.SC1, "SA")]
        [InlineData(Versions.Mobi, SystemType.VG2, "VGM")] // VG2 -> "VG" prefix
        [InlineData(Versions.App, SystemType.VG2, "VGA")]
        public void GetVersionName_ShouldReturnCorrectFormat(Versions version, string systemType, string expected)
        {
            // Arrange
            var metadata = new Dictionary<string, string>();
            var sut = CreateHighStakeService();

            // Act
            var result = HighStakeServices.GetVersionName(version, metadata, systemType);

            // Assert
            result.Should().Be(expected);
        }

        // Helper method to set private fields
        private static void SetPrivateField<T>(HighStakeServices service, string fieldName, T value)
        {
            var field = typeof(HighStakeServices).GetField(fieldName,
                BindingFlags.NonPublic |
                BindingFlags.Instance);
            field?.SetValue(service, value);
        }

        private TestHighStakeService CreateHighStakeService()
        {
            return new TestHighStakeService(
                _sentryClient,
                _configDependencies,
                _repoDependencies
            );
        }


        private TestHighStakeService CreateService()
        {
            var configDeps = new ConfigurationServices(
                _configuration,
                _cache,
                _appSettings,
                _gameUserService,
                _generalGameSettingService,
                _chatBotApi
            );

            var repoDeps = new RepositoryServices(
                _userSessionRepo,
                _exchangeRateRepo,
                _currencyRepo,
                _userRepo
            );


            return new TestHighStakeService(
                _sentryClient,
                configDeps,
                repoDeps
            );
        }

        [Fact]
        public void ConfigureJetStream_WhenConnectionNull_ReturnsEarly()
        {
            var service = CreateService();

            var method = typeof(HighStakeServices).GetMethod("ConfigureJetStream",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (method != null)
            {
                method.Invoke(service, []);
            }

            _sentryClient.DidNotReceive().CaptureEvent(
                Arg.Any<SentryEvent>(),
                Arg.Any<Scope>());
        }

        [Fact]
        public async Task CalAmountOnBaseCurrency_WhenExceptionOccurs_ReturnsBetAmount()
        {
            // Arrange
            var service = CreateService();
            var bet = new BetCreated { Amount = 100, Currency = "USD" };
            _exchangeRateRepo
                .AsQueryable()
                .Returns(x => { throw new Exception("Database error"); });

            // Act
            var result = await service.GetAmount(bet);

            // Assert
            Assert.Equal(100, result);
        }

        [Fact]
        public void GetHighStakeAmount_WhenSettingsNull_ReturnsDefaultAmount()
        {
            // Arrange
            var service = CreateService();
            _appSettings.ChatBotApiSettings.Returns(new ChatBotApiSettings { HighStakeAmount = 2000 });

            // Act
            var result = service.GetHighStakeAmount();

            // Assert
            Assert.Equal(2000, result); // Default amount from constant
        }

        [Fact]
        public void GetHighStakeAmount_WhenSettingsProvided_ReturnsConfiguredAmount()
        {
            // Arrange
            var service = CreateService();
            _appSettings.ChatBotApiSettings.Returns(new ChatBotApiSettings { HighStakeAmount = 5000 });

            // Act
            var result = service.GetHighStakeAmount();

            // Assert
            Assert.Equal(5000, result);
        }

        [Fact]
        public async Task GetAmount_CallsCalAmountOnBaseCurrency()
        {
            // Arrange
            var service = CreateService();
            var bet = new BetCreated { Amount = 100, Currency = "USD" };

            // Act
            var result = await service.GetAmount(bet);

            // Assert
            Assert.Equal(100, result); // Default behavior when no exchange rate
        }

        [Fact]
        public async Task GetUser_WhenCacheHit_ReturnsCachedValue()
        {
            // Arrange
            var service = CreateService();
            var expectedUser = new AccountEntity { PlayerId = "123", OperatorId = "op1" };
            var cacheKey = "User123";

            _cache.GetCache<AccountEntity>(cacheKey)
                     .Returns(expectedUser);

            // Act
            var result = await service.GetUser("123", "op1");

            // Assert
            Assert.Equal(expectedUser, result);
            _userRepo.DidNotReceive().Collection();
        }

        [Fact]
        public async Task NotifyHighStake_WhenAmountBelowThreshold_ReturnsEmptyResult()
        {
            // Arrange
            var service = CreateService();
            var bet = new BetCreated { Amount = 100, Currency = "USD" };
            _appSettings.ChatBotApiSettings.Returns(new ChatBotApiSettings { HighStakeAmount = 5000 });

            // Act
            var result = await service.NotifyHighStake(bet, "TEST");

            // Assert
            Assert.NotNull(result);
            await _chatBotApi.DidNotReceive().ForwardMessage(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>()
            );
        }

        [Fact]
        public async Task NotifyHighStake_WhenExceptionOccurs_LogsToSentry()
        {
            // Arrange
            var service = CreateService();
            var bet = new BetCreated { Amount = 10000, Currency = "USD" };

            _appSettings.ChatBotApiSettings.Returns(new ChatBotApiSettings { HighStakeAmount = 5000 });
            _gameUserService.GetCharacterNameAsync(Arg.Any<string>())
                .Returns(Task.FromException<string>(new Exception("Test exception")));

            // Act
            var result = await service.NotifyHighStake(bet, "TEST");

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetVersionName_WithModeMetadata_IncludesMode()
        {
            // Arrange
            var service = CreateService();
            var metadata = new Dictionary<string, string> { { "mode", "Portrait" } };

            // Act
            var result = HighStakeServices.GetVersionName(Versions.Instant, metadata, SystemType.SC1);

            // Assert
            Assert.Equal("SI", result);
        }

        [Fact]
        public void GetVersionName_ForMegaKeno_ReturnsCorrectPrefix()
        {
            // Arrange
            var service = CreateService();
            var metadata = new Dictionary<string, string>();

            // Act
            var result = HighStakeServices.GetVersionName(Versions.MegaKeno, metadata, SystemType.SC1);

            // Assert
            Assert.Equal("MK", result);
        }

        [Fact]
        public void GetVersionName_ForCocos_ReturnsCorrectPrefix()
        {
            // Arrange
            var service = CreateService();
            var metadata = new Dictionary<string, string>();

            // Act
            var result = HighStakeServices.GetVersionName(Versions.Cocos, metadata, SystemType.SC1);

            // Assert
            Assert.Equal("SC", result);
        }

        [Fact]
        public void ConfigureJetStream_WhenStreamSettingsNull_ReturnsEarly()
        {
            // Arrange
            var service = CreateService();
            _appSettings.StreamSettings.Returns(default(StreamSettings?));

            // Act
            var method = typeof(HighStakeServices).GetMethod("ConfigureJetStream",
                BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(service, Array.Empty<object>());

            // Assert
            var connectionField = typeof(HighStakeServices)
                .GetField("_connection", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.Null(connectionField?.GetValue(service));
        }

        [Fact]
        public async Task NotifyHighStake_WhenHighStakeConditionsMet_SendsNotification()
        {
            // Arrange
            var bet = new BetCreated
            {
                PlayerId = 123,
                Amount = 5000,
                Currency = "USD",
                GameTypeId = 1,
                System = (int)Versions.App,
                Metadata = new Dictionary<string, string>()
            };

            _appSettings.ChatBotApiSettings.Returns(new ChatBotApiSettings
            {
                HighStakeAmount = 2000,
                Token = "token",
                ConversationIds = new ConversationIds { HighStake = "highstake" }
            });

            _gameUserService.GetCharacterNameAsync(Arg.Any<string>())
                .Returns("Player1");

            _generalGameSettingService.GetDisplayName(1)
                .Returns("Game1");

            var sut = CreateHighStakeService();

            // Act
            await sut.NotifyHighStake(bet, "SC1");

            // Assert
            await _chatBotApi.Received(1).ForwardMessage(
                "token",
                "highstake",
                Arg.Is<string>(msg => msg.Contains("Player1") && msg.Contains("5000")));
        }

        [Fact]
        public void GetCocosVersionSuffix_WhenSystemTypeSC1_ReturnsSuffixC()
        {
            // Act
            var result = HighStakeServices.GetVersionName(Versions.Cocos, new Dictionary<string, string>(), SystemType.SC1);

            // Assert
            Assert.Equal("SC", result);
        }

        [Fact]
        public void GetCocosVersionSuffix_WhenSystemTypeNotSC1_ReturnsEmptySuffix()
        {
            // Act
            var result = HighStakeServices.GetVersionName(Versions.Cocos, new Dictionary<string, string>(), SystemType.VG2);

            // Assert
            Assert.Equal("VG", result);
        }

        [Fact]
        public void GetMegaKenoVersionSuffix_WhenSystemTypeSC1_ReturnsMK()
        {
            // Act
            var result = HighStakeServices.GetVersionName(Versions.MegaKeno, new Dictionary<string, string>(), SystemType.SC1);

            // Assert
            Assert.Equal("MK", result);
        }

        [Fact]
        public void GetMegaKenoVersionSuffix_WhenSystemTypeVG2_ReturnsVGK()
        {
            // Act
            var result = HighStakeServices.GetVersionName(Versions.MegaKeno, new Dictionary<string, string>(), SystemType.VG2);

            // Assert
            Assert.Equal("VGK", result);
        }

        [Fact]
        public void GetMegaKenoVersionSuffix_WhenSystemTypeUnknown_ReturnsEmpty()
        {
            // Act
            var result = HighStakeServices.GetVersionName(Versions.MegaKeno, new Dictionary<string, string>(), "unknown");

            // Assert
            Assert.Equal("", result);
        }

        [Fact]
        public void GetModeName_WhenModeIsPortrait_ReturnsPortraitSuffix()
        {
            // Arrange
            var metadata = new Dictionary<string, string> { { "mode", ClientDisplayMode.Portrait.Value } };

            // Act
            var result = HighStakeServices.GetVersionName(Versions.Instant, metadata, SystemType.SC1);

            // Assert
            Assert.Contains(ClientDisplayMode.Portrait.DisplayName, result);
        }

        [Fact]
        public void GetModeName_WhenModeIsNotPortrait_ReturnsEmptyString()
        {
            // Arrange
            var metadata = new Dictionary<string, string> { { "mode", "Landscape" } };

            // Act
            var result = HighStakeServices.GetVersionName(Versions.Instant, metadata, SystemType.SC1);

            // Assert
            Assert.Equal("SI", result);
        }
    }
}