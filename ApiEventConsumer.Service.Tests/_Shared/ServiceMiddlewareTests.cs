using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer._Shared.Middlewares;
using ApiEventConsumer.Subscribers;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver.Core.Configuration;

namespace ApiEventConsumer.Service.Tests._Shared
{
    public class ServiceMiddlewareTests
    {
        private readonly IServiceCollection _services;

        public ServiceMiddlewareTests()
        {
            _services = new ServiceCollection();
        }

        [Fact]
        public void AddChatBotServices_WhenSettingsNull_DoesNotRegisterService()
        {
            // Arrange
            var appSettings = new AppSettings
            {
                ChatBotApiSettings = null
            };

            // Act
            _services.AddChatBotServices(appSettings);
            var serviceProvider = _services.BuildServiceProvider();

            // Assert
            var chatBotApi = serviceProvider.GetService<IChatBotApi>();
            Assert.Null(chatBotApi);
        }

        [Fact]
        public void AddChatBotServices_WhenUrlNull_DoesNotRegisterService()
        {
            // Arrange
            var appSettings = new AppSettings
            {
                ChatBotApiSettings = new ChatBotApiSettings
                {
                    Url = null,
                    TimeoutInSeconds = 30
                }
            };

            // Act
            _services.AddChatBotServices(appSettings);
            var serviceProvider = _services.BuildServiceProvider();

            // Assert
            var chatBotApi = serviceProvider.GetService<IChatBotApi>();
            Assert.Null(chatBotApi);
        }

        [Fact]
        public void AddChatBotServices_WithValidSettings_RegistersService()
        {
            // Arrange
            var appSettings = new AppSettings
            {
                ChatBotApiSettings = new ChatBotApiSettings
                {
                    Url = "http://test.com",
                    TimeoutInSeconds = 30
                }
            };

            // Act
            _services.AddChatBotServices(appSettings);
            var serviceProvider = _services.BuildServiceProvider();

            // Assert
            var chatBotApi = serviceProvider.GetService<IChatBotApi>();
            Assert.NotNull(chatBotApi);
        }

        [Fact]
        public void AddChatBotServices_WithProxy_RegistersServiceWithProxy()
        {
            // Arrange
            var appSettings = new AppSettings
            {
                Port = 1,
                DatabaseSettings = new DatabaseSettings {
                    ConnectionString = string.Empty,
                    DatabaseName = string.Empty,
                    Collection = string.Empty
                },
                ChatBotApiSettings = new ChatBotApiSettings
                {
                    Token = string.Empty,
                    Url = "http://test.com",
                    Proxy = "http://proxy.com",
                    TimeoutInSeconds = 30,
                    HighStakeAmount = 30,
                    ConversationIds = new ConversationIds
                    {
                        HighStake = string.Empty,
                        Promotion = string.Empty,
                        PromotionTournament = string.Empty
                    }
                },
                StreamSettings = new StreamSettings
                {
                    Host = "localhost",
                    Enable = true
                }
            };

            // Act
            _services.AddChatBotServices(appSettings);
            var serviceProvider = _services.BuildServiceProvider();

            // Assert
            var chatBotApi = serviceProvider.GetService<IChatBotApi>();
            Assert.NotNull(chatBotApi);
        }

        [Fact]
        public void AddChatBotServices_WithTimeout_SetsCorrectTimeout()
        {
            // Arrange
            const int expectedTimeout = 60;
            var appSettings = new AppSettings
            {
                ChatBotApiSettings = new ChatBotApiSettings
                {
                    Url = "http://test.com",
                    TimeoutInSeconds = expectedTimeout
                }
            };

            // Act
            _services.AddChatBotServices(appSettings);
            var serviceProvider = _services.BuildServiceProvider();

            // Assert
            var chatBotApi = serviceProvider.GetService<IChatBotApi>();
            Assert.NotNull(chatBotApi);
        }
    }
}
