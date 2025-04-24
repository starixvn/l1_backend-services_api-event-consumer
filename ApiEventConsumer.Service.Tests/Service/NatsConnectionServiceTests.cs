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
        private readonly ISentryClient _sentryClient;
        private readonly IAppSettings _appSettings;
        private readonly IConnection _connection;
        private readonly IJetStream _jsContext;
        private readonly IJetStreamManagement _jsManagement;
        private readonly IAsyncSubscription _subscription;
        private readonly StreamSettings _streamSettings;

        public NatsConnectionServiceTests()
        {
            _sentryClient = Substitute.For<ISentryClient>();
            _appSettings = Substitute.For<IAppSettings>();
            _connection = Substitute.For<IConnection>();
            _jsContext = Substitute.For<IJetStream>();
            _jsManagement = Substitute.For<IJetStreamManagement>();
            _subscription = Substitute.For<IAsyncSubscription>();

            _streamSettings = new StreamSettings
            {
                Enable = true,
                Host = "nats://localhost:4222",
                StreamName = "test_stream",
                BetMessages = "bet.created"
            };
        }

        [Fact]
        public void Disconnect_DisposesConnectionAndSubscription()
        {
            // Arrange
            var service = new NatsConnectionService(_sentryClient, _appSettings);
            var subscriptionField = typeof(NatsConnectionService)
                .GetField("subscription", BindingFlags.NonPublic | BindingFlags.Instance);
            var connectionField = typeof(NatsConnectionService)
                .GetField("connection", BindingFlags.NonPublic | BindingFlags.Instance);

            subscriptionField?.SetValue(service, _subscription);
            connectionField?.SetValue(service, _connection);

            // Act
            service.Disconnect();

            // Assert
            _connection.Received(1).Dispose();
        }

        [Fact]
        public void ConfigureJetStream_WhenConnectionExists_ConfiguresStreamAndSubscription()
        {
            // Arrange
            _appSettings.StreamSettings.Returns(_streamSettings);
            _connection.CreateJetStreamContext(Arg.Any<JetStreamOptions>()).Returns(_jsContext);
            _connection.CreateJetStreamManagementContext(Arg.Any<JetStreamOptions>())
                .Returns(_jsManagement);
            
            var service = new NatsConnectionService(_sentryClient, _appSettings);
            var connectionField = typeof(NatsConnectionService)
                .GetField("connection", BindingFlags.NonPublic | BindingFlags.Instance);
            connectionField?.SetValue(service, _connection);

            // Act
            service.ConfigureJetStreamBetMessage(async (_, __) => await Task.FromResult(true));

            // Assert
            _jsManagement.Received(1).GetStreamInfo(Arg.Any<string>());
            _jsContext.Received(1).PushSubscribeAsync(
                Arg.Any<string>(),
                Arg.Any<EventHandler<MsgHandlerEventArgs>>(),
                false,
                Arg.Any<PushSubscribeOptions>()
            );
        }
    }
}
