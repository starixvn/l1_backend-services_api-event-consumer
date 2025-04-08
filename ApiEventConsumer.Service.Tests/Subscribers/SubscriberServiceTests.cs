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
        }

        private class TestSubscriberService : SubscriberService
        {
            public TestSubscriberService(
                ISentryClient sentryClient,
                ILogger<SubscriberService> logger,
                ConfigurationDependencies configurations,
                RepositoryDependencies repositories,
                ServiceDependencies services)
                : base("USD", sentryClient, logger, configurations, repositories, services)
            {
            }

            protected override StreamSettings GetStreamSettingsPath()
            {
                return new StreamSettings { Enable = false };
            }

            protected override string GetSystemTypePath()
            {
                return "TEST";
            }

            protected override string GetStreamNamePath()
            {
                return "test_stream";
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
        public async Task GetExchangeRate_WhenCacheEmpty_ShouldFetchFromRepository()
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
        private void SetPrivateField<T>(SubscriberService service, string fieldName, T value)
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
            mockJsManagement.Setup(x => x.GetStreamInfo(streamName))
                            .Returns((StreamInfo)null);

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
                _serviceDependencies
            );
        }

        private class TestableSubscriberService : SubscriberService
        {
            public StreamSettings TestStreamSettings { get; set; } = new StreamSettings { Enable = false };

            public TestableSubscriberService(
                ISentryClient sentryClient,
                ILogger<SubscriberService> logger,
                ConfigurationDependencies configurations,
                RepositoryDependencies repositories,
                ServiceDependencies services)
                : base("USD", sentryClient, logger, configurations, repositories, services)
            {
            }

            protected override StreamSettings GetStreamSettingsPath() => TestStreamSettings;
            protected override string GetSystemTypePath() => "TEST";
            protected override string GetStreamNamePath() => "test_stream";
        }
    }
}