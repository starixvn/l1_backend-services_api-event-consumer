using System.Reflection;
using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Service.Service;
using Moq;
using NATS.Client;
using NATS.Client.JetStream;
using Sentry;
using Sentry.Protocol;

namespace ApiEventConsumer.Service.Tests.Service
{
    public class NatsConnectionServiceTests
    {
        private readonly Mock<ISentryClient> _mockSentryClient;
        private readonly Mock<IAppSettings> _mockAppSettings;
        private readonly Mock<IConnection> _mockConnection;
        private readonly Mock<IJetStream> _mockJsContext;
        private readonly Mock<IJetStreamManagement> _mockJsManagement;
        private readonly Mock<IAsyncSubscription> _mockSubscription;
        private readonly StreamSettings _streamSettings;

        public NatsConnectionServiceTests()
        {
            _mockSentryClient = new Mock<ISentryClient>();
            _mockAppSettings = new Mock<IAppSettings>();
            _mockConnection = new Mock<IConnection>();
            _mockJsContext = new Mock<IJetStream>();
            _mockJsManagement = new Mock<IJetStreamManagement>();
            _mockSubscription = new Mock<IAsyncSubscription>();

            _streamSettings = new StreamSettings
            {
                Enable = true,
                Host = "nats://localhost:4222",
                StreamName = "test_stream",
                BetMessages = "bet.created"
            };
        }

        [Fact]
        public void ConfigureStream_WhenStreamInfoNull_AddsNewStream()
        {
            // Arrange
            _mockAppSettings.Setup(x => x.StreamSettings).Returns(_streamSettings);
            _mockConnection.Setup(x => x.CreateJetStreamManagementContext(It.IsAny<JetStreamOptions>())).Returns(_mockJsManagement.Object);
            _mockJsManagement.Setup(x => x.GetStreamInfo(It.IsAny<string>(), It.IsAny<StreamInfoOptions>())).Returns(() => new StreamInfo("test", false));

            var service = new NatsConnectionService(_mockSentryClient.Object, _mockAppSettings.Object);

            // Act
            var method = typeof(NatsConnectionService).GetMethod("ConfigureStream", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(service, [ _mockJsManagement.Object, _streamSettings.StreamName! ]);

            // Assert
            _mockJsManagement.Verify(x => x.AddStream(It.IsAny<StreamConfiguration>()), Times.Once);
        }

        [Fact]
        public void Disconnect_DisposesConnectionAndSubscription()
        {
            // Arrange
            var service = new NatsConnectionService(_mockSentryClient.Object, _mockAppSettings.Object);
            var subscriptionField = typeof(NatsConnectionService)
                .GetField("subscription", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var connectionField = typeof(NatsConnectionService)
                .GetField("connection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            subscriptionField?.SetValue(service, _mockSubscription.Object);
            connectionField?.SetValue(service, _mockConnection.Object);

            // Act
            service.Disconnect();

            // Assert
            _mockSubscription.Verify(x => x.Unsubscribe(), Times.Once);
            _mockConnection.Verify(x => x.Dispose(), Times.Once);
        }

        [Fact]
        public void ConfigureJetStream_WhenConnectionExists_ConfiguresStreamAndSubscription()
        {
            // Arrange
            _mockAppSettings.Setup(x => x.StreamSettings).Returns(_streamSettings);
            _mockConnection.Setup(x => x.CreateJetStreamContext(It.IsAny<JetStreamOptions>())).Returns(_mockJsContext.Object);
            _mockConnection.Setup(x => x.CreateJetStreamManagementContext(It.IsAny<JetStreamOptions>()))
                .Returns(_mockJsManagement.Object);
            
            var service = new NatsConnectionService(_mockSentryClient.Object, _mockAppSettings.Object);
            var connectionField = typeof(NatsConnectionService)
                .GetField("connection", BindingFlags.NonPublic | BindingFlags.Instance);
            connectionField?.SetValue(service, _mockConnection.Object);

            // Act
            service.ConfigureJetStream(async (_, __) => await Task.FromResult(true));

            // Assert
            _mockJsManagement.Verify(x => x.GetStreamInfo(It.IsAny<string>()), Times.Once);
            _mockJsContext.Verify(x => x.PushSubscribeAsync(
                It.IsAny<string>(),
                It.IsAny<EventHandler<MsgHandlerEventArgs>>(),
                false,
                It.IsAny<PushSubscribeOptions>()
            ), Times.Once);
        }
    }
}
