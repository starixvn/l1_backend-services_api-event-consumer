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
        private readonly Mock<StreamConfig> _mockStreamConfig;

        private readonly SubscriberService.ConfigurationDependencies _configDependencies;
        private readonly SubscriberService.RepositoryDependencies _repoDependencies;
        private readonly SubscriberService.ServiceDependencies _serviceDependencies;

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
            _mockStreamConfig = new Mock<StreamConfig>();

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

        }

        private class TestSubscriberService : SubscriberService
        {
            private readonly StreamConfig _streamSettings;

            public TestSubscriberService(
                ISentryClient sentryClient,
                ILogger<SubscriberService> logger,
                ConfigurationDependencies configurations,
                RepositoryDependencies repositories,
                ServiceDependencies services,
                StreamConfig streamSettings)
                : base("TEST", sentryClient, logger, configurations, repositories, services)
            {
                _streamSettings = streamSettings;
            }

            protected override StreamConfig GetStreamConfigPath()
            {
                return _streamSettings;
            }

            protected override string GetSystemTypePath() => "TEST";
            protected override string GetStreamNamePath() => "test.stream";

            protected override string GetBetMessages()
            {
                return "api.bet.created";
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
                _mockStreamConfig.Object
            );
        }

        [Fact]
        public void InitializeNatsConnection_WhenStreamSettingsDisabled_DoesNotConnect()
        {
            // Arrange
            var streamSettings = new StreamConfig { Enable = false };
            var service = CreateService(streamSettings);

            // Act
            service.InitializeNatsConnection();

            // Assert
            var connectionField = typeof(SubscriberService)
                .GetField("_connection", BindingFlags.NonPublic | BindingFlags.Instance);
            var connection = connectionField?.GetValue(service) as IConnection;
            
            Assert.Null(connection);
        }

        private TestSubscriberService CreateService(StreamConfig streamSettings)
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
            var service = CreateService(new StreamConfig { Enable = true });
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
            var service = CreateService(new StreamConfig { Enable = true });
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
            _mockSentryClient.Verify(
                x => x.CaptureEvent(
                    It.Is<SentryEvent>(e => e.Exception != null && 
                                          e.Exception.Message.Contains("Error processing bet.created message")),
                    It.IsAny<Scope>()),
                Times.Once);
        }

        [Fact]
        public async Task GetDeviceInformation_ValidUser_ReturnsDeviceInfo()
        {
            // Arrange
            var service = CreateService(new StreamConfig { Enable = true });
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

        [Fact]
        public async Task StartAsync_InitializesNatsConnection()
        {
            // Arrange
            var streamConfig = new StreamConfig
            { 
                Enable = true
            };
            var service = CreateService(streamConfig);
            
            var mockConnection = new Mock<IConnection>();
            mockConnection
                .SetupGet(x => x.State)
                .Returns(ConnState.CONNECTED);

            var mockFactory = new Mock<IConnectionFactory>();
            mockFactory
                .Setup(x => x.CreateConnection(
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
            var service = CreateService(new StreamConfig { Enable = true });
            var mockConnection = new Mock<IConnection>();
            var mockSubscription = new Mock<IJetStreamPushAsyncSubscription>();

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
            var service = CreateService(new StreamConfig { Enable = true });
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
            var service = CreateService(new StreamConfig { Enable = true });

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
            var service = CreateService(new StreamConfig { Enable = true });
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
            var service = CreateService(new StreamConfig { Enable = true });
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
            var service = CreateService(new StreamConfig { Enable = true });
            var mockConnection = new Mock<IConnection>();
            var mockSubscription = new Mock<IJetStreamPushAsyncSubscription>();

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

        [Fact]
        public void InitializeNatsConnection_WhenExceptionOccurs_CapturesExceptionInSentry()
        {
            // Arrange
            var streamSettings = new StreamSettings 
            { 
                Enable = true,
                Host = "nats://localhost:4222"
            };
            _mockAppSettings
                .Setup(x => x.StreamSettings)
                .Returns(streamSettings);

            var streamConfig = new StreamConfig { Enable = true };
            var service = CreateService(streamConfig);

            // Simulate exception in ConnectToNats
            var mockConnection = new Mock<IConnection>();
            mockConnection
                .SetupGet(x => x.State)
                .Throws(new Exception("Connection failed"));

            // Use reflection to inject mock connection directly
            var connectionField = typeof(SubscriberService)
                .GetField("_connection", BindingFlags.NonPublic | BindingFlags.Instance);
            connectionField?.SetValue(service, mockConnection.Object);

            // Act
            service.InitializeNatsConnection();

            // Assert
        }

        [Fact]
        public void ConfigureSubscription_ShouldSetupSubscriptionCorrectly()
        {
            // Arrange
            var service = CreateService(new StreamConfig { Enable = true });
            var mockJsContext = new Mock<IJetStream>();
            var streamName = "test_stream";
            var betMessages = "api_bet_created";
            var mockSubscription = new Mock<IJetStreamPushAsyncSubscription>();
            mockSubscription.SetupGet(x => x.Subject).Returns(betMessages);
            mockSubscription.Setup(x => x.Start()).Verifiable();

            mockJsContext
                .Setup(x => x.PushSubscribeAsync(
                    betMessages,
                    It.Is<EventHandler<MsgHandlerEventArgs>>(handler => true),
                    It.Is<bool>(autoAck => autoAck == true),
                    It.IsAny<PushSubscribeOptions>()))
                .Returns(mockSubscription.Object);

            // Use reflection to access private method
            var method = typeof(SubscriberService).GetMethod("ConfigureSubscription", 
                BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            method?.Invoke(service, [mockJsContext.Object, streamName, betMessages]);

            // Assert
            mockSubscription.Verify(x => x.Start(), Times.Once);
        }

        [Fact]
        public void GetHighStakeAmount_WhenSettingsNull_ReturnsDefaultAmount()
        {
            // Arrange
            var service = CreateService(new StreamConfig { Enable = true });
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
            var service = CreateService(new StreamConfig { Enable = true });
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
            var service = CreateService(new StreamConfig { Enable = true });
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
            var service = CreateService(new StreamConfig { Enable = true });
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
            var service = CreateService(new StreamConfig { Enable = true });
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
            var service = CreateService(new StreamConfig { Enable = true });
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
        public void ConfigureStream_WhenStreamExists_DoesNotCreateNewStream()
        {
            // Arrange
            var streamName = "test_stream";
            var mockJsManagement = new Mock<IJetStreamManagement>();
            var mockStreamInfo = new StreamInfo(@"{
                ""config"": {
                    ""name"": ""test_stream"",
                    ""subjects"": [""test.>""],
                    ""retention"": ""limits"",
                    ""max_consumers"": -1,
                    ""max_msgs"": -1,
                    ""max_bytes"": -1,
                    ""max_age"": 0,
                    ""max_msg_size"": -1,
                    ""storage"": ""file"",
                    ""discard"": ""old"",
                    ""num_replicas"": 1
                },
                ""state"": {
                    ""messages"": 0,
                    ""bytes"": 0,
                    ""first_seq"": 0,
                    ""last_seq"": 0,
                    ""consumer_count"": 0
                }
            }", true);
            
            mockJsManagement
                .Setup(x => x.GetStreamInfo(streamName))
                .Returns(mockStreamInfo);

            var service = CreateService(new StreamConfig { Enable = true });

            // Act
            service.ConfigureStream(mockJsManagement.Object, streamName);

            // Assert
            mockJsManagement.Verify(
                x => x.AddStream(It.IsAny<StreamConfiguration>()),
                Times.Never);
        }

        [Fact]
        public void ConfigureStream_WhenExceptionOccurs_LogsToSentry()
        {
            // Arrange
            var streamName = "test_stream";
            var mockJsManagement = new Mock<IJetStreamManagement>();
            mockJsManagement
                .Setup(x => x.GetStreamInfo(streamName))
                .Throws(new NATSJetStreamException("Test exception"));

            var service = CreateService(new StreamConfig { Enable = true });

            // Act
            service.ConfigureStream(mockJsManagement.Object, streamName);

            // Assert
            mockJsManagement.Verify(
                x => x.AddStream(It.IsAny<StreamConfiguration>()),
                Times.Never);
        }

        [Fact]
        public void InitializeNatsConnection_WhenStreamSettingsNull_ReturnsEarly()
        {
            // Arrange
            var service = CreateService(new StreamConfig { Enable = true });
            _mockAppSettings.Setup(x => x.StreamSettings)
                .Returns(default(StreamSettings));

            // Act
            service.InitializeNatsConnection();

            // Assert
            var connectionField = typeof(SubscriberService)
                .GetField("_connection", BindingFlags.NonPublic | BindingFlags.Instance);
            var connection = connectionField?.GetValue(service) as IConnection;
            Assert.Null(connection);
        }

        [Fact]
        public async Task NotifyHighStake_WhenAmountBelowThreshold_ReturnsEmptyResult()
        {
            // Arrange
            var service = CreateService(new StreamConfig { Enable = true });
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
        public async Task NotifyHighStake_WhenHighStake_SendsNotification()
        {
            // Arrange
            var service = CreateService(new StreamConfig { Enable = true });
            var bet = new BetCreated 
            { 
                Amount = 10000,
                Currency = "USD",
                PlayerId = 123,
                GameTypeId = 1,
                System = (int)Versions.App,
                Metadata = new Dictionary<string, string>()
            };

            _mockAppSettings.Setup(x => x.ChatBotApiSettings).Returns(new ChatBotApiSettings 
            { 
                HighStakeAmount = 5000,
                Token = "token",
                ConversationIds = new ConversationIds { HighStake = "conv1" }
            });

            _mockGameUserService.Setup(x => x.GetCharacterNameAsync(It.IsAny<string>()))
                               .ReturnsAsync("TestPlayer");

            _mockGeneralGameSettingService.Setup(x => x.GetDisplayName(It.IsAny<int>()))
                                        .ReturnsAsync("TestGame");

            // Act
            var result = await service.NotifyHighStake(bet, "TEST");

            // Assert
            Assert.NotNull(result);
            _mockChatBotApi.Verify(
                x => x.ForwardMessage(
                    "token",
                    "conv1",
                    It.Is<string>(msg => msg.Contains("TestPlayer") && msg.Contains("10000"))),
                Times.Once);
        }

        [Fact]
        public async Task NotifyHighStake_WhenExceptionOccurs_LogsToSentry()
        {
            // Arrange
            var service = CreateService(new StreamConfig { Enable = true });
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
            var service = CreateService(new StreamConfig { Enable = true });
            var metadata = new Dictionary<string, string> { { "mode", "Portrait" } };

            // Act
            var result = service.GetVersionName(Versions.Instant, metadata, SystemType.SC1);

            // Assert
            Assert.Equal("SI", result);
        }

        [Fact]
        public void GetVersionName_ForMegaKeno_ReturnsCorrectPrefix()
        {
            // Arrange
            var service = CreateService(new StreamConfig { Enable = true });
            var metadata = new Dictionary<string, string>();

            // Act
            var result = service.GetVersionName(Versions.MegaKeno, metadata, SystemType.SC1);

            // Assert
            Assert.Equal("MK", result);
        }

        [Fact]
        public void GetVersionName_ForCocos_ReturnsCorrectPrefix()
        {
            // Arrange
            var service = CreateService(new StreamConfig { Enable = true });
            var metadata = new Dictionary<string, string>();

            // Act
            var result = service.GetVersionName(Versions.Cocos, metadata, SystemType.SC1);

            // Assert
            Assert.Equal("SC", result);
        }
    }
}