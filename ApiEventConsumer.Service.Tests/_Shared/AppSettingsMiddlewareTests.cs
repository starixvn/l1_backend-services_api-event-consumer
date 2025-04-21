using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer._Shared.Middlewares;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace ExchangeRate.Service.Tests._Shared
{
    public class AppSettingsMiddlewareTests
    {
        [Fact]
        public void AddSettings_Always_AddSingletonIAppSettings()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = Substitute.For<IConfiguration>();

            // Act
            AppSettingsMiddleware.AddSettings(services, configuration);
            var provider = services.BuildServiceProvider();
            var result = provider.GetRequiredService<IAppSettings>();

            // Assert
            Assert.NotNull(result);
        }
    }
}