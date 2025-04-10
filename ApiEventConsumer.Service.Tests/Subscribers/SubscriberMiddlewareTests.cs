using ApiEventConsumer.Database;
using ApiEventConsumer.Database.Entities;
using ApiEventConsumer.Database.Entities.Currencies;
using ApiEventConsumer.Database.Entities.ExchangeRates;
using ApiEventConsumer.Subscribers;
using Microsoft.Extensions.DependencyInjection;

namespace ApiEventConsumer.Service.Tests.Subscribers
{
    public class SubscriberMiddlewareTests
    {
        private readonly IServiceCollection _services;
        private readonly SubscriberSettings _settings;

        public SubscriberMiddlewareTests()
        {
            _services = new ServiceCollection();
            _settings = new SubscriberSettings();
        }

        [Fact]
        public void AddSubcriberServices_RegistersSubscriberSettings()
        {
            // Arrange
            var settings = new SubscriberSettings
            {
                SC = new SubcriberDatabase { SystemType = "SC" },
                VG = new SubcriberDatabase { SystemType = "VG" },
                GN = new SubcriberDatabase { SystemType = "GN" }
            };

            // Act
            _services.AddSubcriberServices(settings);
            var serviceProvider = _services.BuildServiceProvider();

            // Assert
            var registeredSettings = serviceProvider.GetService<SubscriberSettings>();
            Assert.NotNull(registeredSettings);
            Assert.NotNull(registeredSettings.SC);
            Assert.NotNull(registeredSettings.VG);
            Assert.NotNull(registeredSettings.GN);
            Assert.Equal("SC", registeredSettings.SC.SystemType);
            Assert.Equal("VG", registeredSettings.VG.SystemType);
            Assert.Equal("GN", registeredSettings.GN.SystemType);
        }

        [Fact]
        public void AddSubcriberServices_RegistersRepositoriesAsSingleton()
        {
            // Act
            _services.AddSubcriberServices(_settings);

            // Assert
            AssertServiceLifetime<IGenericRepository<UserSessionEntity>>(ServiceLifetime.Singleton);
            AssertServiceLifetime<IGenericRepository<ExchangeRateEntity>>(ServiceLifetime.Singleton);
            AssertServiceLifetime<IGenericRepository<CurrencyEntity>>(ServiceLifetime.Singleton);
            AssertServiceLifetime<IGenericRepository<AccountEntity>>(ServiceLifetime.Singleton);
        }

        private void AssertServiceLifetime<T>(ServiceLifetime expectedLifetime)
        {
            var serviceDescriptor = _services.FirstOrDefault(d => d.ServiceType == typeof(T));
            Assert.NotNull(serviceDescriptor);
            Assert.Equal(expectedLifetime, serviceDescriptor.Lifetime);
        }
    }
}
