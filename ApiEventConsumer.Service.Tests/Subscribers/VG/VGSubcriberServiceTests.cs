using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Subscribers;
using ApiEventConsumer.Subscribers.VG;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using Sentry;
using Microsoft.Extensions.Configuration;
using ApiEventConsumer.Api._Shared.Helpers;
using ApiEventConsumer.Database.Entities;
using ApiEventConsumer.Database;
using ApiEventConsumer.Database.Entities.ExchangeRates;
using ApiEventConsumer.Database.Entities.Currencies;
using ApiEventConsumer.Service.Service;

namespace ApiEventConsumer.Service.Tests.Subscribers.VG
{
    public class VGSubcriberServiceTests
    {
        private readonly Mock<ISentryClient> _mockSentryClient;
        private readonly Mock<ILogger<SubscriberService>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ICacheManager> _mockCache;
        private readonly Mock<IAppSettings> _mockAppSettings;
        private readonly Mock<IGenericRepository<UserSessionEntity>> _mockUserSessionRepo;
        private readonly Mock<IGenericRepository<ExchangeRateEntity>> _mockExchangeRateRepo;
        private readonly Mock<IGenericRepository<CurrencyEntity>> _mockCurrencyRepo;
        private readonly Mock<IGenericRepository<AccountEntity>> _mockAccountRepo;
        private readonly Mock<IGameUserService> _mockGameUserService;
        private readonly Mock<IGeneralGameSettingService> _mockGeneralGameSettingService;
        private readonly Mock<IChatBotApi> _mockChatBotApi;

        private readonly SubscriberService.ConfigurationDependencies _configDependencies;
        private readonly SubscriberService.RepositoryDependencies _repoDependencies;
        private readonly SubscriberService.ServiceDependencies _serviceDependencies;

        public VGSubcriberServiceTests()
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
            _mockAccountRepo = new Mock<IGenericRepository<AccountEntity>>();
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
                _mockAccountRepo.Object
            );

            _serviceDependencies = new SubscriberService.ServiceDependencies(
                _mockGameUserService.Object,
                _mockGeneralGameSettingService.Object,
                _mockChatBotApi.Object
            );
        }

        [Fact]
        public void GetBetMessages_WhenSettingsNull_ReturnsEmptyString()
        {
            // Arrange
            _mockAppSettings.Setup(x => x.SubcriberSettings).Returns(new SubscriberSettings());
            var service = CreateService();

            // Act
            var result = GetProtectedValue<string>(service, "GetBetMessages");

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void GetBetMessages_WhenSettingsExist_ReturnsCorrectValue()
        {
            // Arrange
            var expectedMessage = "test.message";
            var settings = new SubscriberSettings
            {
                VG = new SubcriberDatabase
                {
                    StreamConfig = new StreamConfig
                    {
                        BetMessages = expectedMessage
                    }
                }
            };
            _mockAppSettings.Setup(x => x.SubcriberSettings).Returns(settings);
            var service = CreateService();

            // Act
            var result = GetProtectedValue<string>(service, "GetBetMessages");

            // Assert
            result.Should().Be(expectedMessage);
        }

        [Fact]
        public void GetStreamNamePath_WhenSettingsNull_ReturnsEmptyString()
        {
            // Arrange
            _mockAppSettings.Setup(x => x.SubcriberSettings).Returns(new SubscriberSettings());
            var service = CreateService();

            // Act
            var result = GetProtectedValue<string>(service, "GetStreamNamePath");

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void GetStreamConfigPath_WhenSettingsNull_ReturnsNewStreamConfig()
        {
            // Arrange
            _mockAppSettings.Setup(x => x.SubcriberSettings).Returns(new SubscriberSettings());
            var service = CreateService();

            // Act
            var result = GetProtectedValue<StreamConfig>(service, "GetStreamConfigPath");

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<StreamConfig>();
        }

        private VGSubcriberService CreateService()
        {
            return new VGSubcriberService(
                _mockSentryClient.Object,
                _mockLogger.Object,
                _configDependencies,
                _repoDependencies,
                _serviceDependencies
            );
        }

        private static T GetProtectedValue<T>(VGSubcriberService service, string methodName)
        {
            var method = typeof(VGSubcriberService).GetMethod(methodName, 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            return (T)method?.Invoke(service, null)!;
        }
    }
}
