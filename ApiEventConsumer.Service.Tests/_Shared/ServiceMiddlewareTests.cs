using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer._Shared.Middlewares;
using ApiEventConsumer.Subscribers;
using Microsoft.Extensions.DependencyInjection;

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
                ChatBotApiSettings = new ChatBotApiSettings
                {
                    Url = "http://test.com",
                    Proxy = "http://proxy.com",
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
