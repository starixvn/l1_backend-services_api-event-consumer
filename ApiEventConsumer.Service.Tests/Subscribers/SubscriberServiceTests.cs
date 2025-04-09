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

namespace ApiEventConsumer.Service.Tests.Subscribers
{
    public class SubscriberServiceTests
    {
        private readonly Mock<ISentryClient> _mockSentryClient;
        private readonly Mock<ILogger<SubscriberService>> _mockLogger;
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
        private readonly Mock<StreamSettings> _mockStreamSettings;
        private readonly Mock<IConnectionFactory> _mockConnectionFactory;

        private readonly SubscriberService.ConfigurationDependencies _configDependencies;
        private readonly SubscriberService.RepositoryDependencies _repoDependencies;
        private readonly SubscriberService.ServiceDependencies _serviceDependencies;
        private const int CacheExpireInMinutes = 60 * 24;

        public SubscriberServiceTests()
        {
            // Setup mocks
            _mockSentryClient = new Mock<ISentryClient>();
            _mockLogger = new Mock<ILogger<SubscriberService>>();
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
            _mockStreamSettings = new Mock<StreamSettings>();
            _mockConnectionFactory = new Mock<IConnectionFactory>();

            // Setup dependencies
            _configDependencies = new SubscriberService.ConfigurationDependencies(
                _mockConfiguration.Object,
                _mockCache.Object,
                _mockAppSettings.Object
            );

            _repoDependencies = new SubscriberService.RepositoryDependencies(
                _mockUserSessionRepo.Object,
                _mockExchangeRateRepo.Object,
                _mockCurrencyRepo.Object,
                _mockUserRepo.Object
            );

            _serviceDependencies = new SubscriberService.ServiceDependencies(
                _mockGameUserService.Object,
                _mockGeneralGameSettingService.Object,
                _mockChatBotApi.Object
            );

            // Correct way to setup nested properties
            _mockAppSettings
                .SetupGet(x => x.CloudSettings)
                .Returns(new CloudSettings { IsCloudService = true });
        }

        private class TestSubscriberService : SubscriberService
        {
            private readonly StreamSettings _streamSettings;

            public TestSubscriberService(
                ISentryClient sentryClient,
                ILogger<SubscriberService> logger,
                ConfigurationDependencies configurations,
                RepositoryDependencies repositories,
                ServiceDependencies services,
                StreamSettings streamSettings)
                : base("TEST", sentryClient, logger, configurations, repositories, services)
            {
                _streamSettings = streamSettings;
            }

            protected override StreamSettings GetStreamSettingsPath()
            {
                return _streamSettings;
            }

            protected override string GetSystemTypePath() => "TEST";
            protected override string GetStreamNamePath() => "test.stream";

            protected override string GetBetMessages()
            {
                throw new NotImplementedException();
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
        public void GetExchangeRate_WhenCacheEmpty_ShouldFetchFromRepository()
        {
            // Arrange
            var expectedRate = new ExchangeRateEntity();
            var mockQueryable = new List<ExchangeRateEntity> { expectedRate }.AsQueryable();

            _mockCache.Setup(x => x.GetCache<ExchangeRateEntity>(It.IsAny<string>()))
                     .Returns(default(ExchangeRateEntity));

            _mockExchangeRateRepo.Setup(x => x.AsQueryable())
                                .Returns(mockQueryable);

            var sut = CreateSubscriberService();

            // Act
            var result = sut.GetExchangeRate();

            // Assert
            result.Should().Be(expectedRate);
            _mockCache.Verify(x => x.SetCache(
                It.IsAny<string>(),
                It.IsAny<ExchangeRateEntity>(),
                It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public async Task GetGameName_WhenKenoMatch1_ShouldReturnKenoSoccer()
        {
            // Arrange
            var sut = CreateSubscriberService();

            // Act
            var result = await sut.GetGameName((int)GameId.KenoMatch1);

            // Assert
            result.Should().Be("KenoSoccer");
            _mockGeneralGameSettingService.Verify(x => x.GetDisplayName(It.IsAny<int>()), Times.Never);
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
            var result = SubscriberService.GetSystemPrefixName(systemType);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public async Task CalAmountOnBaseCurrency_WhenExchangeRateNull_ShouldReturnOriginalAmount()
        {
            // Arrange
            var bet = new BetCreated { Amount = 100 };
            var exchangeRate = new ExchangeRateEntity { Code = null };
            
            _mockCache.Setup(x => x.GetCache<ExchangeRateEntity>(CachedKeys.ExchangeRate.ToString()))
                    .Returns(exchangeRate);
            
            var sut = CreateSubscriberService();

            // Act
            var result = await sut.CalAmountOnBaseCurrency(bet);

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
            var result = sut.GetVersionName(version, metadata, systemType);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void Dispose_ShouldCleanupResources()
        {
            // Arrange
            var mockConnection = new Mock<IConnection>();
            var mockBetCreatedSub = new Mock<IAsyncSubscription>();
            
            var sut = CreateSubscriberService();
            // Set private fields using reflection
            SetPrivateField(sut, "_connection", mockConnection.Object);
            SetPrivateField(sut, "_betCreatedSub", mockBetCreatedSub.Object);

            // Act
            sut.Dispose();
            sut.Dispose(); // Second call should not throw

            // Assert
            mockBetCreatedSub.Verify(x => x.Unsubscribe(), Times.Once);
            mockConnection.Verify(x => x.Dispose(), Times.Once);
        }

        // Helper method to set private fields
        private static void SetPrivateField<T>(SubscriberService service, string fieldName, T value)
        {
            var field = typeof(SubscriberService).GetField(fieldName, 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            field?.SetValue(service, value);
        }

        [Fact]
        public void ConfigureStream_WhenStreamDoesNotExist_ShouldCreateNewStream()
        {
            // Arrange
            var streamName = "test_stream";
            var mockJsManagement = new Mock<IJetStreamManagement>();
            mockJsManagement
                .Setup(x => x.GetStreamInfo(streamName))
                .Returns(() => null!);

            var sut = CreateSubscriberService();

            // Act
            sut.ConfigureStream(mockJsManagement.Object, streamName);

            // Assert
            mockJsManagement.Verify(
                x => x.AddStream(It.Is<StreamConfiguration>(c => c.Name == streamName)),
                Times.Once);
        }


        [Fact]
        public async Task StartAsync_ShouldInitializeNatsConnection()
        {
            // Arrange
            var sut = CreateSubscriberService();
            var cancellationToken = new CancellationToken();

            // Act
            await sut.StartAsync(cancellationToken);

            // Assert
            // Verify that InitializeNatsConnection was called
            // This might require refactoring the service to make it testable
        }

        [Fact]
        public async Task StopAsync_ShouldDisposeResources()
        {
            // Arrange
            var sut = CreateSubscriberService();
            var cancellationToken = new CancellationToken();

            // Act
            await sut.StopAsync(cancellationToken);

            // Assert
            // Verify that resources were disposed
            // This might require refactoring the service to make it testable
        }

        private TestSubscriberService CreateSubscriberService()
        {
            return new TestSubscriberService(
                _mockSentryClient.Object,
                _mockLogger.Object,
                _configDependencies,
                _repoDependencies,
                _serviceDependencies,
                _mockStreamSettings.Object
            );
        }

        [Fact]
        public void InitializeNatsConnection_WhenStreamSettingsDisabled_DoesNotConnect()
        {
            // Arrange
            var streamSettings = new StreamSettings { Enable = false };
            var service = CreateService(streamSettings);

            // Act
            service.InitializeNatsConnection();

            // Assert
            var connectionField = typeof(SubscriberService)
                .GetField("_connection", BindingFlags.NonPublic | BindingFlags.Instance);
            var connection = connectionField?.GetValue(service) as IConnection;
            
            Assert.Null(connection);
        }

        private TestSubscriberService CreateService(StreamSettings streamSettings)
        {
            var configDeps = new SubscriberService.ConfigurationDependencies(
                _mockConfiguration.Object,
                _mockCache.Object,
                _mockAppSettings.Object
            );

            var repoDeps = new SubscriberService.RepositoryDependencies(
                _mockUserSessionRepo.Object,
                _mockExchangeRateRepo.Object,
                _mockCurrencyRepo.Object,
                _mockUserRepo.Object
            );

            var serviceDeps = new SubscriberService.ServiceDependencies(
                _mockGameUserService.Object,
                _mockGeneralGameSettingService.Object,
                _mockChatBotApi.Object
            );

            return new TestSubscriberService(
                _mockSentryClient.Object,
                _mockLogger.Object,
                configDeps,
                repoDeps,
                serviceDeps,
                streamSettings
            );
        }

        [Fact]
        public void HandleBetCreated_ValidMessage_ProcessesSuccessfully()
        {
            // Arrange
            var service = CreateService(new StreamSettings { Enable = true });
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
        public void HandleBetCreated_InvalidMessage_HandlesError()
        {
            // Arrange
            var service = CreateService(new StreamSettings { Enable = true });
            var invalidMessage = "invalid json";
            var msgArgs = new MsgHandlerEventArgs(
                new Msg("subject", Encoding.UTF8.GetBytes(invalidMessage)));

            // Setup Sentry client
            _mockSentryClient
                .Setup(x => x.IsEnabled)
                .Returns(true);

            // Act
            service.HandleBetCreated(null, msgArgs);

            // Assert
        }

        [Fact]
        public async Task GetDeviceInformation_ValidUser_ReturnsDeviceInfo()
        {
            // Arrange
            var service = CreateService(new StreamSettings { Enable = true });
            var userSession = new UserSessionEntity
            {
                PlayerId = "123",
                BrowserUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)",
                Ip = "127.0.0.1",
                RecheckInTime = DateTime.UtcNow
            };

            _mockUserSessionRepo
                .Setup(x => x.Collection())
                .Returns(MockMongoCollection(new[] { userSession }));

            // Act
            var result = await service.GetDeviceInformation("123");

            // Assert
        }

        [Fact]
        public async Task StartAsync_InitializesNatsConnection()
        {
            // Arrange
            var streamSettings = new StreamSettings 
            { 
                Enable = true,
                Host = "nats://localhost:4222"
            };
            var service = CreateService(streamSettings);
            
            var mockConnection = new Mock<IConnection>();
            mockConnection
                .SetupGet(x => x.State)
                .Returns(ConnState.CONNECTED);

            var mockFactory = new Mock<IConnectionFactory>();
            mockFactory
                .Setup(x => x.CreateConnection(
                    It.Is<string>(url => url == streamSettings.Host),
                    It.Is<bool>(reconnect => reconnect == false)))
                .Returns(mockConnection.Object);

            // Use reflection to inject mock factory
            var factoryField = typeof(SubscriberService)
                .GetField("_connectionFactory", BindingFlags.NonPublic | BindingFlags.Instance);
            factoryField?.SetValue(service, mockFactory.Object);

            // Act
            await service.StartAsync(CancellationToken.None);

            // Assert
            var connectionField = typeof(SubscriberService)
                .GetField("_connection", BindingFlags.NonPublic | BindingFlags.Instance);
            var actualConnection = connectionField?.GetValue(service) as IConnection;
        }

        [Fact]
        public async Task StopAsync_DisposesAllResources()
        {
            // Arrange
            var service = CreateService(new StreamSettings { Enable = true });
            var mockConnection = new Mock<IConnection>();
            var mockSubscription = new Mock<IAsyncSubscription>();

            // Use reflection to set private fields
            var connectionField = typeof(SubscriberService)
                .GetField("_connection", BindingFlags.NonPublic | BindingFlags.Instance);
            var subField = typeof(SubscriberService)
                .GetField("_betCreatedSub", BindingFlags.NonPublic | BindingFlags.Instance);

            connectionField?.SetValue(service, mockConnection.Object);
            subField?.SetValue(service, mockSubscription.Object);

            // Act
            await service.StopAsync(CancellationToken.None);

            // Assert
            mockConnection.Verify(x => x.Dispose(), Times.Once);
            mockSubscription.Verify(x => x.Unsubscribe(), Times.Once);
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
            var service = CreateService(new StreamSettings { Enable = true });
            _mockUserSessionRepo
                .Setup(x => x.Collection())
                .Returns(MockMongoCollection<UserSessionEntity>(new List<UserSessionEntity>()));

            // Act
            var result = await service.GetDeviceInformation("123");

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void ConfigureJetStream_WhenConnectionNull_ReturnsEarly()
        {
            var service = CreateService(new StreamSettings { Enable = true });

            var method = typeof(SubscriberService).GetMethod("ConfigureJetStream", 
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
            var service = CreateService(new StreamSettings { Enable = true });
            var bet = new BetCreated { Amount = 100, Currency = "USD" };
            _mockExchangeRateRepo
                .Setup(x => x.AsQueryable())
                .Throws(new Exception("Database error"));

            // Act
            var result = await service.CalAmountOnBaseCurrency(bet);

            // Assert
            Assert.Equal(100, result);
        }

        [Theory]
        [InlineData(GameId.KenoMatch1, "KenoSoccer")]
        [InlineData(GameId.KenoMatch2, "KenoSoccer2")]
        [InlineData(GameId.KenoPower1, "KenoPower")]
        [InlineData((GameId)999, null)]
        public async Task GetGameName_ForDifferentGames_ReturnsCorrectName(GameId gameId, string? expectedName)
        {
            // Arrange
            var service = CreateService(new StreamSettings { Enable = true });
            if (expectedName == null)
            {
                _mockGeneralGameSettingService
                    .Setup(x => x.GetDisplayName(It.IsAny<int>()))
                    .ReturnsAsync("Unknown Game");
                expectedName = "Unknown Game";
            }

            // Act
            var result = await service.GetGameName((int)gameId);

            // Assert
            Assert.Equal(expectedName, result);
        }

        [Fact]
        public void Dispose_WhenCalledMultipleTimes_OnlyDisposesOnce()
        {
            // Arrange
            var service = CreateService(new StreamSettings { Enable = true });
            var mockConnection = new Mock<IConnection>();
            var mockSubscription = new Mock<IAsyncSubscription>();

            // Use reflection to set private fields
            var connectionField = typeof(SubscriberService)
                .GetField("_connection", BindingFlags.NonPublic | BindingFlags.Instance);
            var subField = typeof(SubscriberService)
                .GetField("_betCreatedSub", BindingFlags.NonPublic | BindingFlags.Instance);

            connectionField?.SetValue(service, mockConnection.Object);
            subField?.SetValue(service, mockSubscription.Object);

            // Act
            service.Dispose();
            service.Dispose();

            // Assert
            mockConnection.Verify(x => x.Dispose(), Times.Once);
            mockSubscription.Verify(x => x.Unsubscribe(), Times.Once);
        }
    }
}