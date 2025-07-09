using ApiEventConsumer.Service.Service;
using ApiEventConsumer.Subscribers;
using NATS.Client;
using NSubstitute;
using Xunit;

namespace ApiEventConsumer.Service.Tests.Subscribers
{
    public class SubscriberManagerTests
    {
        private readonly INatsConnectionService _natsService;
        private readonly IEventSubscriber _mockSubscriber;
        private readonly SubscriberManager _sut;

        public SubscriberManagerTests()
        {
            _natsService = Substitute.For<INatsConnectionService>();
            _mockSubscriber = Substitute.For<IEventSubscriber>();
            _sut = new SubscriberManager(_natsService);
        }

        [Fact]
        public void Constructor_ShouldInitializeNatsConnection()
        {
            // Assert
            _natsService.Received(1).InitializeConnection();
        }

        [Fact]
        public void RegisterSubscriber_ShouldAddSubscriberToList()
        {
            // Act
            _sut.RegisterSubscriber(_mockSubscriber);

            // Assert
            Assert.Equal(_mockSubscriber, _sut.GetSubscriber<IEventSubscriber>());
        }

        [Fact]
        public void RegisterAllSubscribers_ShouldConfigureJetStreamForEachSubscriber()
        {
            // Arrange
            const string subjectName = "test.subject";
            _mockSubscriber.SubjectName.Returns(subjectName);
            _sut.RegisterSubscriber(_mockSubscriber);

            // Act
            _sut.RegisterAllSubscribers();

            // Assert
            _natsService.Received(1).ConfigureJetStream(
                Arg.Any<Func<object?, MsgHandlerEventArgs, Task<bool>>>(),
                subjectName
            );
        }

        [Fact]
        public void GetSubscriber_WhenSubscriberExists_ShouldReturnSubscriber()
        {
            // Arrange
            _sut.RegisterSubscriber(_mockSubscriber);

            // Act
            var result = _sut.GetSubscriber<IEventSubscriber>();

            // Assert
            Assert.Equal(_mockSubscriber, result);
        }

        [Fact]
        public void GetSubscriber_WhenSubscriberDoesNotExist_ShouldThrowException()
        {
            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => _sut.GetSubscriber<IEventSubscriber>()
            );
            Assert.Contains("No subscriber of type IEventSubscriber found", exception.Message);
        }

        [Fact]
        public async Task RegisterAllSubscribers_ShouldCallOnMessageReceivedWhenHandlingMessage()
        {
            // Arrange
            _mockSubscriber.SubjectName.Returns("test.subject");
            _mockSubscriber.OnMessageReceived(Arg.Any<object>(), Arg.Any<MsgHandlerEventArgs>()).Returns(Task.FromResult(true));
            _sut.RegisterSubscriber(_mockSubscriber);

            // Capture the message handler when ConfigureJetStream is called
            Func<object?, MsgHandlerEventArgs, Task<bool>> capturedHandler = null!;
            _natsService.ConfigureJetStream(
                Arg.Do<Func<object?, MsgHandlerEventArgs, Task<bool>>>(x => capturedHandler = x),
                Arg.Any<string>()
            );

            // Act
            _sut.RegisterAllSubscribers();

            // Create test message and invoke the captured handler
            var mockArgs = Substitute.For<MsgHandlerEventArgs>(new Msg());
            var result = await capturedHandler(null, mockArgs);

            // Assert
            Assert.True(result);
            await _mockSubscriber.Received(1).OnMessageReceived(Arg.Any<object>(), mockArgs);
        }
    }
}
