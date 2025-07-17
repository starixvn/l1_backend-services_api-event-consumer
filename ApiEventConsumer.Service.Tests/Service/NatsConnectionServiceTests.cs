using System.Reflection;
using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Service.Service;
using NSubstitute;
using NATS.Client;
using NATS.Client.JetStream;
using Sentry;

namespace ApiEventConsumer.Service.Tests.Service
{
    public class NatsConnectionServiceTests
    {
        [Fact]
        public async Task Disconnect_DisposesConnectionAndSubscriptions()
        {
            // Arrange
            var sentryClient = Substitute.For<ISentryClient>();
            var appSettings = Substitute.For<IAppSettings>();
            var connection = Substitute.For<IConnection>();
            var pullSubscription = Substitute.For<IJetStreamPullSubscription>();

            var service = new NatsConnectionService(sentryClient, appSettings);
            var subscriptionsField = typeof(NatsConnectionService)
                .GetField("subscriptions", BindingFlags.NonPublic | BindingFlags.Instance);
            var connectionField = typeof(NatsConnectionService)
                .GetField("connection", BindingFlags.NonPublic | BindingFlags.Instance);

            var subscriptions = new List<IJetStreamPullSubscription> { pullSubscription };
            subscriptionsField?.SetValue(service, subscriptions);
            connectionField?.SetValue(service, connection);

            // Act
            await service.DisconnectAsync();

            // Assert
            connection.Received(1).Dispose();
            pullSubscription.Received(1).Unsubscribe();
        }

        [Fact]
        public void InitializeConnection_WhenStreamSettingsDisabled_DoesNotConnect()
        {
            // Arrange
            var sentryClient = Substitute.For<ISentryClient>();
            var appSettings = Substitute.For<IAppSettings>();
            
            var streamSettings = new StreamSettings
            {
                Enable = false,
                Host = "nats://localhost:4222",
                StreamName = "test_stream",
                BetMessages = "bet.created"
            };
            
            appSettings.StreamSettings.Returns(streamSettings);

            // Act
            var service = new NatsConnectionService(sentryClient, appSettings);
            service.InitializeConnection();

            // Assert - Verify no exceptions were captured without using Arg.Any
            sentryClient.DidNotReceiveWithAnyArgs().CaptureException(default(Exception)!);
        }

        [Fact]
        public void ConfigureJetStream_WhenConnectionExists_DoesNotThrow()
        {
            // Arrange
            var sentryClient = Substitute.For<ISentryClient>();
            var appSettings = Substitute.For<IAppSettings>();
            var connection = Substitute.For<IConnection>();
            var jsContext = Substitute.For<IJetStream>();
            var jsManagement = Substitute.For<IJetStreamManagement>();
            
            var streamSettings = new StreamSettings
            {
                Enable = true,
                Host = "nats://localhost:4222",
                StreamName = "test_stream",
                BetMessages = "bet.created"
            };

            appSettings.StreamSettings.Returns(streamSettings);
            connection.CreateJetStreamContext().Returns(jsContext);
            connection.CreateJetStreamManagementContext().Returns(jsManagement);

            var service = new NatsConnectionService(sentryClient, appSettings);
            var connectionField = typeof(NatsConnectionService)
                .GetField("connection", BindingFlags.NonPublic | BindingFlags.Instance);
            connectionField?.SetValue(service, connection);

            Assert.NotNull(streamSettings.BetMessages);

            // Act & Assert - Should not throw
            var exception = Record.Exception(() => 
                service.ConfigureJetStream(async (_, __) => await Task.FromResult(true), streamSettings.BetMessages));
            
            Assert.Null(exception);
        }

        [Fact]
        public void IsHealthy_WhenConnectionIsNull_ReturnsFalse()
        {
            // Arrange
            var sentryClient = Substitute.For<ISentryClient>();
            var appSettings = Substitute.For<IAppSettings>();
            var service = new NatsConnectionService(sentryClient, appSettings);

            // Act
            var result = service.IsHealthy();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsHealthy_WhenConnectionIsClosed_ReturnsFalse()
        {
            // Arrange
            var sentryClient = Substitute.For<ISentryClient>();
            var appSettings = Substitute.For<IAppSettings>();
            var connection = Substitute.For<IConnection>();
            
            var service = new NatsConnectionService(sentryClient, appSettings);
            var connectionField = typeof(NatsConnectionService)
                .GetField("connection", BindingFlags.NonPublic | BindingFlags.Instance);
            connectionField?.SetValue(service, connection);

            connection.IsClosed().Returns(true);

            // Act
            var result = service.IsHealthy();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsHealthy_WhenNoSubscriptions_ReturnsFalse()
        {
            // Arrange
            var sentryClient = Substitute.For<ISentryClient>();
            var appSettings = Substitute.For<IAppSettings>();
            var connection = Substitute.For<IConnection>();
            
            var service = new NatsConnectionService(sentryClient, appSettings);
            var connectionField = typeof(NatsConnectionService)
                .GetField("connection", BindingFlags.NonPublic | BindingFlags.Instance);
            connectionField?.SetValue(service, connection);

            connection.IsClosed().Returns(false);

            // Act
            var result = service.IsHealthy();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsHealthy_WhenSubscriptionInvalid_ReturnsFalse()
        {
            // Create a completely isolated test to avoid any NSubstitute state pollution
            var service = CreateNatsConnectionServiceWithMocks(out var connection, out var pullSubscription, out var _, out var _);
            
            // Configure mock behaviors
            connection.IsClosed().Returns(false);
            pullSubscription.IsValid.Returns(false);
            
            // Setup the service with the mocked connection and subscription
            SetupServiceWithConnectionAndSubscriptions(service, connection, new List<IJetStreamPullSubscription> { pullSubscription });

            // Act
            var result = service.IsHealthy();

            // Assert
            Assert.False(result);
        }

        private static NatsConnectionService CreateNatsConnectionServiceWithMocks(
            out IConnection connection,
            out IJetStreamPullSubscription pullSubscription,
            out ISentryClient sentryClient,
            out IAppSettings appSettings)
        {
            sentryClient = Substitute.For<ISentryClient>();
            appSettings = Substitute.For<IAppSettings>();
            connection = Substitute.For<IConnection>();
            pullSubscription = Substitute.For<IJetStreamPullSubscription>();
            
            return new NatsConnectionService(sentryClient, appSettings);
        }

        private static void SetupServiceWithConnectionAndSubscriptions(
            NatsConnectionService service,
            IConnection connection,
            List<IJetStreamPullSubscription> subscriptions)
        {
            var connectionField = typeof(NatsConnectionService)
                .GetField("connection", BindingFlags.NonPublic | BindingFlags.Instance);
            var subscriptionsField = typeof(NatsConnectionService)
                .GetField("subscriptions", BindingFlags.NonPublic | BindingFlags.Instance);

            connectionField?.SetValue(service, connection);
            subscriptionsField?.SetValue(service, subscriptions);
        }

        [Fact]
        public void IsHealthy_WhenAllConditionsAreHealthy_ReturnsTrue()
        {
            // Arrange
            var sentryClient = Substitute.For<ISentryClient>();
            var appSettings = Substitute.For<IAppSettings>();
            var connection = Substitute.For<IConnection>();
            var pullSubscription = Substitute.For<IJetStreamPullSubscription>();
            
            var service = new NatsConnectionService(sentryClient, appSettings);
            var connectionField = typeof(NatsConnectionService)
                .GetField("connection", BindingFlags.NonPublic | BindingFlags.Instance);
            var subscriptionsField = typeof(NatsConnectionService)
                .GetField("subscriptions", BindingFlags.NonPublic | BindingFlags.Instance);

            connectionField?.SetValue(service, connection);
            var subscriptions = new List<IJetStreamPullSubscription> { pullSubscription };
            subscriptionsField?.SetValue(service, subscriptions);

            connection.IsClosed().Returns(false);
            pullSubscription.IsValid.Returns(true);

            // Act
            var result = service.IsHealthy();

            // Assert
            Assert.True(result);
        }
    }
}
