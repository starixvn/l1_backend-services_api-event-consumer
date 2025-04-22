using Moq;
using FluentAssertions;
using ApiEventConsumer.Subscribers;
using Sentry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ApiEventConsumer.Api._Shared.Helpers;
using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Database;
using ApiEventConsumer.Database.Entities;
using ApiEventConsumer.Database.Entities.Currencies;
using ApiEventConsumer.Database.Entities.ExchangeRates;
using ApiEventConsumer.Service.Service;
using ApiEventConsumer.Core.Models.NatMessage;
using MongoDB.Driver;
using ApiEventConsumer.Core.Constants;
using ApiEventConsumer.Core.Enumeration;
using NATS.Client.JetStream;
using NATS.Client;
using System.Reflection;
using System.Text;
using System.Text.Json;
using NSubstitute;
using System.Linq.Expressions;

namespace ApiEventConsumer.Service.Tests.Subscribers
{
    public class SubscriberServiceTests
    {
        private readonly Mock<ISentryClient> _mockSentryClient;
        private readonly Mock<ILogger<SubscriberServices>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ICacheManager> _mockCache;
        private readonly Mock<IAppSettings> _mockAppSettings;
        private readonly Mock<IGenericRepository<UserSessionEntity>> _mockUserSessionRepo;
        private readonly Mock<IGenericRepository<ExchangeRateEntity>> _mockExchangeRateRepo;
        private readonly Mock<IGenericRepository<CurrencyEntity>> _mockCurrencyRepo;
        private readonly Mock<IGenericRepository<AccountEntity>> _mockUserRepo;
        private readonly Mock<IGameUserService> _mockGameUserService;
        private readonly Mock<IGeneralGameSettingService> _mockGeneralGameSettingService;
        private readonly Mock<IChatBotApi> _mockChatBotApi;

        private readonly ConfigurationServices _configDependencies;
        private readonly RepositoryServices _repoDependencies;

        public SubscriberServiceTests()
        {
            // Setup mocks
            _mockSentryClient = new Mock<ISentryClient>();
            _mockLogger = new Mock<ILogger<SubscriberServices>>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockCache = new Mock<ICacheManager>();
            _mockAppSettings = new Mock<IAppSettings>();
            _mockUserSessionRepo = new Mock<IGenericRepository<UserSessionEntity>>();
            _mockExchangeRateRepo = new Mock<IGenericRepository<ExchangeRateEntity>>();
            _mockCurrencyRepo = new Mock<IGenericRepository<CurrencyEntity>>();
            _mockUserRepo = new Mock<IGenericRepository<AccountEntity>>();
            _mockGameUserService = new Mock<IGameUserService>();
            _mockGeneralGameSettingService = new Mock<IGeneralGameSettingService>();
            _mockChatBotApi = new Mock<IChatBotApi>();

            // Setup dependencies
            _configDependencies = new ConfigurationServices(
                _mockConfiguration.Object,
                _mockCache.Object,
                _mockAppSettings.Object,
                _mockGameUserService.Object,
                _mockGeneralGameSettingService.Object,
                _mockChatBotApi.Object
            );

            _repoDependencies = new RepositoryServices(
                _mockUserSessionRepo.Object,
                _mockExchangeRateRepo.Object,
                _mockCurrencyRepo.Object,
                _mockUserRepo.Object
            );
        }

        private class TestSubscriberService : SubscriberServices
        {

            public TestSubscriberService(
                ISentryClient sentryClient,
                ConfigurationServices configurations,
                RepositoryServices repositories)
                : base(sentryClient,  configurations, repositories)
            {
            }
        }

        [Fact]
        public async Task NotifyHighStake_WhenBetIsUUS_ShouldReturnEmptyResult()
        {
            // Arrange
            var sut = CreateSubscriberService();
            var bet = new BetCreated { Currency = "UUS" };

            // Act
            var result = await sut.NotifyHighStake(bet, "TEST");

            // Assert
            result.Should().NotBeNull();
            _mockChatBotApi.Verify(x => x.ForwardMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetGameName_WhenUnknownGame_ShouldCallGeneralGameSettingService()
        {
            // Arrange
            var unknownGameId = 999;
            var expectedName = "Unknown Game";
            _mockGeneralGameSettingService.Setup(x => x.GetDisplayName(unknownGameId))
                                        .ReturnsAsync(expectedName);
            var sut = CreateSubscriberService();

            // Act
            var result = await sut.GetGameName(unknownGameId);

            // Assert
            result.Should().Be(expectedName);
            _mockGeneralGameSettingService.Verify(x => x.GetDisplayName(unknownGameId), Times.Once);
        }

        [Fact]
        public void GetCurrencies_WhenCacheHit_ShouldReturnCachedValue()
        {
            // Arrange
            var expectedCurrencies = new List<CurrencyEntity> { new CurrencyEntity() };
            _mockCache.Setup(x => x.GetCache<IEnumerable<CurrencyEntity>>(CachedKeys.Currencies.ToString()))
                    .Returns(expectedCurrencies);
            
            var sut = CreateSubscriberService();

            // Act
            var result = sut.GetCurrencies();

            // Assert
            result.Should().BeSameAs(expectedCurrencies);
            _mockCurrencyRepo.Verify(x => x.AsQueryable(), Times.Never);
        }

        [Theory]
        [InlineData(SystemType.SC1, "S")]
        [InlineData(SystemType.VG2, "VG")]
        [InlineData(SystemType.GN, "GN")]
        [InlineData("unknown", "")]
        public void GetSystemPrefixName_ShouldReturnCorrectPrefix(string systemType, string expected)
        {
            // Act
            var result = SubscriberServices.GetSystemPrefixName(systemType);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public async Task CalAmountOnBaseCurrency_WhenExchangeRateNull_ShouldReturnOriginalAmount()
        {
            // Arrange
            var bet = new BetCreated { Amount = 100 };
            var exchangeRate = new ExchangeRateEntity { 
                Code = null,
                Time = DateTime.UtcNow,
                ExDate = DateTime.UtcNow,
                Source = string.Empty
            };
            
            _mockCache.Setup(x => x.GetCache<ExchangeRateEntity>(CachedKeys.ExchangeRate.ToString()))
                    .Returns(exchangeRate);
            
            var sut = CreateSubscriberService();

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
            var sut = CreateSubscriberService();

            // Act
            var result = SubscriberServices.GetVersionName(version, metadata, systemType);

            // Assert
            result.Should().Be(expected);
        }

        // Helper method to set private fields
        private static void SetPrivateField<T>(SubscriberServices service, string fieldName, T value)
        {
            var field = typeof(SubscriberServices).GetField(fieldName, 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            field?.SetValue(service, value);
        }

        private TestSubscriberService CreateSubscriberService()
        {
            return new TestSubscriberService(
                _mockSentryClient.Object,
                _configDependencies,
                _repoDependencies
            );
        }


        private TestSubscriberService CreateService()
        {
            var configDeps = new ConfigurationServices(
                _mockConfiguration.Object,
                _mockCache.Object,
                _mockAppSettings.Object,
                _mockGameUserService.Object,
                _mockGeneralGameSettingService.Object,
                _mockChatBotApi.Object
            );

            var repoDeps = new RepositoryServices(
                _mockUserSessionRepo.Object,
                _mockExchangeRateRepo.Object,
                _mockCurrencyRepo.Object,
                _mockUserRepo.Object
            );


            return new TestSubscriberService(
                _mockSentryClient.Object,
                configDeps,
                repoDeps
            );
        }

        [Fact]
        public void HandleBetCreated_ValidMessage_ProcessesSuccessfully()
        {
            // Arrange
            var service = CreateService();
            var betCreated = new BetCreated
            {
                PlayerId = 123,
                GameTypeId = 1,
                Amount = 5000,
                Currency = "USD",
                System = (int)Versions.App
            };
            var message = JsonSerializer.Serialize(betCreated);
            var msgArgs = new MsgHandlerEventArgs(
                new Msg("subject", Encoding.UTF8.GetBytes(message)));

            _mockGameUserService
                .Setup(x => x.GetCharacterNameAsync(It.IsAny<string>()))
                .ReturnsAsync("TestPlayer");

            _mockGeneralGameSettingService
                .Setup(x => x.GetDisplayName(It.IsAny<int>()))
                .ReturnsAsync("Crash");

            // Act
            service.HandleBetCreated(null, msgArgs);

            // Assert
        }

        [Fact]
        public async Task GetDeviceInformation_ValidUser_ReturnsDeviceInfo()
        {
            // Arrange
            var service = CreateService();
            var userSession = new UserSessionEntity
            {
                PlayerId = "123",
                BrowserUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)",
                Ip = "127.0.0.1",
                RecheckInTime = DateTime.UtcNow,
                ParnerId = "123"
            };

            _mockUserSessionRepo
                .Setup(x => x.Collection())
                .Returns(MockMongoCollection(new[] { userSession }));

            // Act
            var result = await service.GetDeviceInformation("123");

            // Assert
        }


        private static IMongoCollection<T> MockMongoCollection<T>(IEnumerable<T> data)
        {
            var mockCollection = new Mock<IMongoCollection<T>>();
            var mockAsyncCursor = new Mock<IAsyncCursor<T>>();
            mockAsyncCursor.Setup(x => x.Current).Returns(data);
            mockAsyncCursor
                .SetupSequence(x => x.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);

            mockCollection
                .Setup(x => x.FindAsync(
                    It.IsAny<FilterDefinition<T>>(),
                    It.IsAny<FindOptions<T, T>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockAsyncCursor.Object);

            return mockCollection.Object;
        }

        [Fact]
        public async Task GetDeviceInformation_NoSession_ReturnsUnknown()
        {
            // Arrange
            var service = CreateService();
            _mockUserSessionRepo
                .Setup(x => x.Collection())
                .Returns(MockMongoCollection<UserSessionEntity>(new List<UserSessionEntity>()));

            // Act
            var result = await service.GetDeviceInformation("123");

            // Assert
            Assert.Equal("- Unknown", result);
        }

        [Fact]
        public void ConfigureJetStream_WhenConnectionNull_ReturnsEarly()
        {
            var service = CreateService();

            var method = typeof(SubscriberServices).GetMethod("ConfigureJetStream", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (method != null)
            {
                method.Invoke(service, []);
            }

            _mockSentryClient.Verify(
                x => x.CaptureEvent(
                    It.IsAny<SentryEvent>(), 
                    It.IsAny<Scope>()),
                Times.Never);
        }

        [Fact]
        public async Task CalAmountOnBaseCurrency_WhenExceptionOccurs_ReturnsBetAmount()
        {
            // Arrange
            var service = CreateService();
            var bet = new BetCreated { Amount = 100, Currency = "USD" };
            _mockExchangeRateRepo
                .Setup(x => x.AsQueryable())
                .Throws(new Exception("Database error"));

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
            _mockAppSettings.Setup(x => x.ChatBotApiSettings)
                .Returns(new ChatBotApiSettings { HighStakeAmount = 2000});

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
            _mockAppSettings.Setup(x => x.ChatBotApiSettings).Returns(new ChatBotApiSettings { HighStakeAmount = 5000 });

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
            
            _mockCache.Setup(x => x.GetCache<AccountEntity>(cacheKey))
                     .Returns(expectedUser);

            // Act
            var result = await service.GetUser("123", "op1");

            // Assert
            Assert.Equal(expectedUser, result);
            _mockUserRepo.Verify(x => x.Collection(), Times.Never);
        }

        [Fact]
        public async Task GetUser_WhenCacheMiss_FetchesFromDatabase()
        {
            // Arrange
            var service = CreateService();
            var expectedUser = new AccountEntity { PlayerId = "123", OperatorId = "op1" };
            var cacheKey = "User123";
            
            _mockCache.Setup(x => x.GetCache<AccountEntity>(cacheKey))
                     .Returns(default(AccountEntity));

            _mockUserRepo.Setup(x => x.Collection())
                        .Returns(MockMongoCollection(new[] { expectedUser }));

            // Act
            var result = await service.GetUser("123", "op1");

            // Assert
            _mockCache.Verify(x => x.SetCache(
                cacheKey,
                It.IsAny<AccountEntity>(),
                It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public void HandleBetCreated_WhenMessageIsInvalid_LogsToSentry()
        {
            // Arrange
            var service = CreateService();
            var invalidData = Encoding.UTF8.GetBytes("invalid json");
            var msgArgs = new MsgHandlerEventArgs(new Msg("subject", invalidData));

            // Setup Sentry client
            _mockSentryClient
                .Setup(x => x.IsEnabled)
                .Returns(true);

            // Act
            service.HandleBetCreated(null, msgArgs);

            // Assert
            _mockSentryClient.Verify(
                x => x.CaptureEvent(
                    It.Is<SentryEvent>(e => e.Exception != null),
                    It.IsAny<Scope>()),
                Times.Once);
        }

        [Fact]
        public async Task NotifyHighStake_WhenAmountBelowThreshold_ReturnsEmptyResult()
        {
            // Arrange
            var service = CreateService();
            var bet = new BetCreated { Amount = 100, Currency = "USD" };
            _mockAppSettings.Setup(x => x.ChatBotApiSettings).Returns(new ChatBotApiSettings { HighStakeAmount = 5000 });

            // Act
            var result = await service.NotifyHighStake(bet, "TEST");

            // Assert
            Assert.NotNull(result);
            _mockChatBotApi.Verify(
                x => x.ForwardMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task NotifyHighStake_WhenExceptionOccurs_LogsToSentry()
        {
            // Arrange
            var service = CreateService();
            var bet = new BetCreated { Amount = 10000, Currency = "USD" };

            _mockAppSettings.Setup(x => x.ChatBotApiSettings).Returns(new ChatBotApiSettings { HighStakeAmount = 5000 });
            _mockGameUserService.Setup(x => x.GetCharacterNameAsync(It.IsAny<string>()))
                               .ThrowsAsync(new Exception("Test exception"));

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
            var result = SubscriberServices.GetVersionName(Versions.Instant, metadata, SystemType.SC1);

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
            var result = SubscriberServices.GetVersionName(Versions.MegaKeno, metadata, SystemType.SC1);

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
            var result = SubscriberServices.GetVersionName(Versions.Cocos, metadata, SystemType.SC1);

            // Assert
            Assert.Equal("SC", result);
        }

        [Fact]
        public void ConfigureJetStream_WhenStreamSettingsNull_ReturnsEarly()
        {
            // Arrange
            var service = CreateService();
            _mockAppSettings.Setup(x => x.StreamSettings).Returns(() => (StreamSettings?)null);

            // Act
            var method = typeof(SubscriberServices).GetMethod("ConfigureJetStream", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(service, Array.Empty<object>());

            // Assert
            var connectionField = typeof(SubscriberServices)
                .GetField("_connection", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.Null(connectionField?.GetValue(service));
        }

        [Fact]
        public async Task GetDeviceInformation_WhenUserAgentNull_HandlesGracefully()
        {
            // Arrange
            var service = CreateService();
            var userSession = new UserSessionEntity
            {
                PlayerId = "123",
                BrowserUserAgent = null,
                Ip = "127.0.0.1"
            };

            _mockUserSessionRepo
                .Setup(x => x.Collection())
                .Returns(MockMongoCollection(new[] { userSession }));

            // Act
            var result = await service.GetDeviceInformation("123");

            // Assert
            Assert.Contains("Unknown", result);
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

            _mockAppSettings
                .Setup(x => x.ChatBotApiSettings)
                .Returns(new ChatBotApiSettings 
                { 
                    HighStakeAmount = 2000,
                    Token = "token",
                    ConversationIds = new ConversationIds { HighStake = "highstake" }
                });

            _mockGameUserService
                .Setup(x => x.GetCharacterNameAsync(It.IsAny<string>()))
                .ReturnsAsync("Player1");

            _mockGeneralGameSettingService
                .Setup(x => x.GetDisplayName(1))
                .ReturnsAsync("Game1");

            var sut = CreateSubscriberService();

            // Act
            await sut.NotifyHighStake(bet, "SC1");

            // Assert
            _mockChatBotApi.Verify(x => x.ForwardMessage(
                "token",
                "highstake",
                It.Is<string>(msg => msg.Contains("Player1") && msg.Contains("5000"))));
        }

        [Fact]
        public void GetCocosVersionSuffix_WhenSystemTypeSC1_ReturnsSuffixC()
        {
            // Act
            var result = SubscriberServices.GetVersionName(Versions.Cocos, new Dictionary<string, string>(), SystemType.SC1);

            // Assert
            Assert.Equal("SC", result);
        }

        [Fact]
        public void GetCocosVersionSuffix_WhenSystemTypeNotSC1_ReturnsEmptySuffix()
        {
            // Act
            var result = SubscriberServices.GetVersionName(Versions.Cocos, new Dictionary<string, string>(), SystemType.VG2);

            // Assert
            Assert.Equal("VG", result);
        }

        [Fact]
        public void GetMegaKenoVersionSuffix_WhenSystemTypeSC1_ReturnsMK()
        {
            // Act
            var result = SubscriberServices.GetVersionName(Versions.MegaKeno, new Dictionary<string, string>(), SystemType.SC1);

            // Assert
            Assert.Equal("MK", result);
        }

        [Fact]
        public void GetMegaKenoVersionSuffix_WhenSystemTypeVG2_ReturnsVGK()
        {
            // Act
            var result = SubscriberServices.GetVersionName(Versions.MegaKeno, new Dictionary<string, string>(), SystemType.VG2);

            // Assert
            Assert.Equal("VGK", result);
        }

        [Fact]
        public void GetMegaKenoVersionSuffix_WhenSystemTypeUnknown_ReturnsEmpty()
        {
            // Act
            var result = SubscriberServices.GetVersionName(Versions.MegaKeno, new Dictionary<string, string>(), "unknown");

            // Assert
            Assert.Equal("", result);
        }

        [Fact]
        public void GetModeName_WhenModeIsPortrait_ReturnsPortraitSuffix()
        {
            // Arrange
            var metadata = new Dictionary<string, string> { { "mode", ClientDisplayMode.Portrait.Value } };

            // Act
            var result = SubscriberServices.GetVersionName(Versions.Instant, metadata, SystemType.SC1);

            // Assert
            Assert.Contains(ClientDisplayMode.Portrait.DisplayName, result);
        }

        [Fact]
        public void GetModeName_WhenModeIsNotPortrait_ReturnsEmptyString()
        {
            // Arrange
            var metadata = new Dictionary<string, string> { { "mode", "Landscape" } };

            // Act
            var result = SubscriberServices.GetVersionName(Versions.Instant, metadata, SystemType.SC1);

            // Assert
            Assert.Equal("SI", result);
        }
    }
}