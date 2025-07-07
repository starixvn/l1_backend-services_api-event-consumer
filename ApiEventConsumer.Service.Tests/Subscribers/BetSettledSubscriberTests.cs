using System;
using System.Text;
using System.Text.Json;
using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Core.Models.NatMessage;
using ApiEventConsumer.Service.Service;
using ApiEventConsumer.Subscribers.Bet;
using NATS.Client;
using NSubstitute;
using Xunit;

namespace ApiEventConsumer.Service.Tests.Subscribers
{
    public class BetSettledSubscriberTests
    {
        private readonly ISentryClient _sentryClient;
        private readonly ConfigurationServices _configurationServices;
        private readonly IAppSettings _appSettings;
        private readonly BetSettledSubscriber _sut;

        public BetSettledSubscriberTests()
        {
            _sentryClient = Substitute.For<ISentryClient>();
            _configurationServices = Substitute.For<ConfigurationServices>();
            _appSettings = Substitute.For<IAppSettings>();
            
            _configurationServices.AppSettings.Returns(_appSettings);
            _sut = new BetSettledSubscriber(_sentryClient, _configurationServices);
        }

        [Fact]
        public void SubjectName_ShouldReturnBetSettledMessagesFromAppSettings()
        {
            // Arrange
            const string expectedSubject = "test.bet.settled";
            var streamSettings = Substitute.For<StreamSettings>();
            streamSettings.BetSettledMessages = expectedSubject;
            _appSettings.StreamSettings.Returns(streamSettings);

            // Act
            var result = _sut.SubjectName;

            // Assert
            Assert.Equal(expectedSubject, result);
        }

        [Fact]
        public void SubjectName_WhenStreamSettingsIsNull_ShouldReturnEmptyString()
        {
            // Arrange
            _appSettings.StreamSettings.Returns((StreamSettings?)null);

            // Act
            var result = _sut.SubjectName;

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void SubjectName_WhenBetSettledMessagesIsNull_ShouldReturnEmptyString()
        {
            // Arrange
            var streamSettings = Substitute.For<StreamSettings>();
            streamSettings.BetSettledMessages = null;
            _appSettings.StreamSettings.Returns(streamSettings);

            // Act
            var result = _sut.SubjectName;

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void OnMessageReceived_WithValidBetSettledMessage_ShouldProcessSuccessfully()
        {
            // Arrange
            var betSettled = new BetSettled
            {
                GameRoundId = 123,
                GameTypeId = 2,
                TotalAmount = 100.50m,
                TotalWin = 200.00m,
                ValidBetAmount = 95.00m,
                FreeBet = true,
                PromotionId = "promo_123",
                Seq = "seq_123",
                PlayerId = 456,
                SiteId = "site_123"
            };

            var jsonMessage = JsonSerializer.Serialize(betSettled);
            var messageData = Encoding.UTF8.GetBytes(jsonMessage);
            
            var mockMessage = Substitute.For<Msg>();
            mockMessage.Data.Returns(messageData);
            
            var mockArgs = Substitute.For<MsgHandlerEventArgs>(mockMessage);
            _appSettings.SystemType.Returns("TEST");

            // Act
            _sut.OnMessageReceived(this, mockArgs);

            // Assert
            _sentryClient.DidNotReceive().CaptureException(Arg.Any<Exception>());
        }

        [Fact]
        public void OnMessageReceived_WithNullBetSettled_ShouldNotThrowException()
        {
            // Arrange
            var jsonMessage = "null";
            var messageData = Encoding.UTF8.GetBytes(jsonMessage);
            
            var mockMessage = Substitute.For<Msg>();
            mockMessage.Data.Returns(messageData);
            
            var mockArgs = Substitute.For<MsgHandlerEventArgs>(mockMessage);
            _appSettings.SystemType.Returns("TEST");

            // Act
            _sut.OnMessageReceived(this, mockArgs);

            // Assert
            _sentryClient.DidNotReceive().CaptureException(Arg.Any<Exception>());
        }

        [Fact]
        public void OnMessageReceived_WithInvalidJson_ShouldCaptureException()
        {
            // Arrange
            var invalidJson = "invalid json";
            var messageData = Encoding.UTF8.GetBytes(invalidJson);
            
            var mockMessage = Substitute.For<Msg>();
            mockMessage.Data.Returns(messageData);
            
            var mockArgs = Substitute.For<MsgHandlerEventArgs>(mockMessage);
            _appSettings.SystemType.Returns("TEST");

            // Act
            _sut.OnMessageReceived(this, mockArgs);

            // Assert
            _sentryClient.Received(1).CaptureException(Arg.Any<JsonException>());
        }

        [Fact]
        public void OnMessageReceived_WhenExceptionThrown_ShouldCaptureException()
        {
            // Arrange
            var mockMessage = Substitute.For<Msg>();
            mockMessage.Data.Returns(x => throw new Exception("Test exception"));
            
            var mockArgs = Substitute.For<MsgHandlerEventArgs>(mockMessage);

            // Act
            _sut.OnMessageReceived(this, mockArgs);

            // Assert
            _sentryClient.Received(1).CaptureException(Arg.Any<Exception>());
        }

        [Fact]
        public void OnMessageReceived_WithSystemTypeNull_ShouldUseEmptyString()
        {
            // Arrange
            var betSettled = new BetSettled
            {
                GameRoundId = 123,
                GameTypeId = 2,
                TotalAmount = 100.50m,
                TotalWin = 200.00m,
                ValidBetAmount = 95.00m,
                FreeBet = true,
                PromotionId = "promo_123",
                Seq = "seq_123",
                PlayerId = 456,
                SiteId = "site_123"
            };

            var jsonMessage = JsonSerializer.Serialize(betSettled);
            var messageData = Encoding.UTF8.GetBytes(jsonMessage);
            
            var mockMessage = Substitute.For<Msg>();
            mockMessage.Data.Returns(messageData);
            
            var mockArgs = Substitute.For<MsgHandlerEventArgs>(mockMessage);
            _appSettings.SystemType.Returns((string?)null);

            // Act
            _sut.OnMessageReceived(this, mockArgs);

            // Assert
            _sentryClient.DidNotReceive().CaptureException(Arg.Any<Exception>());
        }

        [Fact]
        public void Constructor_ShouldInitializeCorrectly()
        {
            // Act
            var subscriber = new BetSettledSubscriber(_sentryClient, _configurationServices);

            // Assert
            Assert.NotNull(subscriber);
        }
    }
} 