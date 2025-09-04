using System.Text;
using System.Text.Json;
using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Core.Models.NatMessage;
using ApiEventConsumer.Services;
using ApiEventConsumer.Subscribers.Bet;
using FluentAssertions;
using NATS.Client;
using NSubstitute;

namespace ApiEventConsumer.Service.Tests.Subscribers
{
    public class BetSubscriberTests
    {
        [Fact]
        public void SubjectName_WhenStreamSettingsIsNull_ShouldReturnEmptyString()
        {
            // Arrange
            var sentryClient = Substitute.For<ISentryClient>();
            var appSettings = Substitute.For<IAppSettings>();
            var highStakeServices = Substitute.For<IHighStakeServices>();
            
            appSettings.StreamSettings.Returns((StreamSettings?)null);
            
            var betSubscriber = new BetSubscriber(sentryClient, appSettings, highStakeServices);

            // Act
            var result = betSubscriber.SubjectName;

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void SubjectName_WhenBetMessagesIsNull_ShouldReturnEmptyString()
        {
            // Arrange
            var sentryClient = Substitute.For<ISentryClient>();
            var appSettings = Substitute.For<IAppSettings>();
            var highStakeServices = Substitute.For<IHighStakeServices>();
            
            var streamSettings = new StreamSettings { BetMessages = null };
            appSettings.StreamSettings.Returns(streamSettings);
            
            var betSubscriber = new BetSubscriber(sentryClient, appSettings, highStakeServices);

            // Act
            var result = betSubscriber.SubjectName;

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void SubjectName_WhenBetMessagesHasValue_ShouldReturnCorrectValue()
        {
            // Arrange
            var sentryClient = Substitute.For<ISentryClient>();
            var appSettings = Substitute.For<IAppSettings>();
            var highStakeServices = Substitute.For<IHighStakeServices>();
            
            const string expectedSubject = "bet.created";
            var streamSettings = new StreamSettings { BetMessages = expectedSubject };
            appSettings.StreamSettings.Returns(streamSettings);
            
            var betSubscriber = new BetSubscriber(sentryClient, appSettings, highStakeServices);

            // Act
            var result = betSubscriber.SubjectName;

            // Assert
            result.Should().Be(expectedSubject);
        }

        [Theory]
        [InlineData("")]
        [InlineData("  ")]
        [InlineData(null)]
        public void SubjectName_WithVariousBetMessagesValues_ShouldReturnExpectedResult(string? betMessages)
        {
            // Arrange
            var sentryClient = Substitute.For<ISentryClient>();
            var appSettings = Substitute.For<IAppSettings>();
            var highStakeServices = Substitute.For<IHighStakeServices>();
            
            var streamSettings = new StreamSettings { BetMessages = betMessages };
            appSettings.StreamSettings.Returns(streamSettings);
            
            var betSubscriber = new BetSubscriber(sentryClient, appSettings, highStakeServices);

            // Act
            var result = betSubscriber.SubjectName;

            // Assert
            var expected = betMessages ?? string.Empty;
            result.Should().Be(expected);
        }

        [Fact]
        public async Task OnMessageReceived_ValidBetCreatedMessage_ShouldCallHighStakeServicesAndReturnTrue()
        {
            // Arrange
            var sentryClient = Substitute.For<ISentryClient>();
            var appSettings = Substitute.For<IAppSettings>();
            var highStakeServices = Substitute.For<IHighStakeServices>();
            
            var streamSettings = new StreamSettings { BetMessages = "bet.created" };
            appSettings.StreamSettings.Returns(streamSettings);
            appSettings.SystemType.Returns("TEST");
            
            var betSubscriber = new BetSubscriber(sentryClient, appSettings, highStakeServices);

            var betCreated = new BetCreated
            {
                GameRoundId = 1,
                GameTypeId = 2,
                Amount = 1000m,
                Currency = "USD",
                UserId = "user123"
            };

            var args = CreateMessageArgs(betCreated);

            // Act
            var result = await betSubscriber.OnMessageReceived(null, args);

            // Assert
            result.Should().BeTrue();
            await highStakeServices.Received(1).NotifyHighStake(Arg.Is<BetCreated>(b => 
                b.GameRoundId == betCreated.GameRoundId &&
                b.GameTypeId == betCreated.GameTypeId &&
                b.Amount == betCreated.Amount &&
                b.Currency == betCreated.Currency &&
                b.UserId == betCreated.UserId), "TEST");
        }

        [Fact]
        public async Task OnMessageReceived_InvalidJsonMessage_ShouldNotCallHighStakeServicesAndReturnTrue()
        {
            // Arrange
            var sentryClient = Substitute.For<ISentryClient>();
            sentryClient.IsEnabled.Returns(true);
            var appSettings = Substitute.For<IAppSettings>();
            var highStakeServices = Substitute.For<IHighStakeServices>();
            
            var streamSettings = new StreamSettings { BetMessages = "bet.created" };
            appSettings.StreamSettings.Returns(streamSettings);
            appSettings.SystemType.Returns("TEST");
            
            var betSubscriber = new BetSubscriber(sentryClient, appSettings, highStakeServices);

            const string invalidJson = "{ invalid json }";
            var args = CreateMessageArgs(invalidJson);

            // Act
            var result = await betSubscriber.OnMessageReceived(null, args);

            // Assert
            result.Should().BeTrue();
            await highStakeServices.DidNotReceiveWithAnyArgs().NotifyHighStake(default!, default!);
            sentryClient.ReceivedWithAnyArgs(1).CaptureException(default!);
        }

        [Fact]
        public async Task OnMessageReceived_EmptyMessage_ShouldNotCallHighStakeServicesAndReturnTrue()
        {
            // Arrange
            var sentryClient = Substitute.For<ISentryClient>();
            sentryClient.IsEnabled.Returns(true);
            var appSettings = Substitute.For<IAppSettings>();
            var highStakeServices = Substitute.For<IHighStakeServices>();
            
            var streamSettings = new StreamSettings { BetMessages = "bet.created" };
            appSettings.StreamSettings.Returns(streamSettings);
            appSettings.SystemType.Returns("TEST");
            
            var betSubscriber = new BetSubscriber(sentryClient, appSettings, highStakeServices);

            var args = CreateMessageArgs(string.Empty);

            // Act
            var result = await betSubscriber.OnMessageReceived(null, args);

            // Assert
            result.Should().BeTrue();
            await highStakeServices.DidNotReceiveWithAnyArgs().NotifyHighStake(default!, default!);
            sentryClient.ReceivedWithAnyArgs(1).CaptureException(default!);
        }

        [Fact]
        public async Task OnMessageReceived_NullBetCreated_ShouldNotCallHighStakeServicesAndReturnTrue()
        {
            // Arrange
            var sentryClient = Substitute.For<ISentryClient>();
            var appSettings = Substitute.For<IAppSettings>();
            var highStakeServices = Substitute.For<IHighStakeServices>();
            
            var streamSettings = new StreamSettings { BetMessages = "bet.created" };
            appSettings.StreamSettings.Returns(streamSettings);
            appSettings.SystemType.Returns("TEST");
            
            var betSubscriber = new BetSubscriber(sentryClient, appSettings, highStakeServices);

            const string nullJson = "null";
            var args = CreateMessageArgs(nullJson);

            // Act
            var result = await betSubscriber.OnMessageReceived(null, args);

            // Assert
            result.Should().BeTrue();
            await highStakeServices.DidNotReceiveWithAnyArgs().NotifyHighStake(default!, default!);
        }

        [Fact]
        public async Task OnMessageReceived_SystemTypeIsNull_ShouldUseEmptyStringForSystemType()
        {
            // Arrange
            var sentryClient = Substitute.For<ISentryClient>();
            var appSettings = Substitute.For<IAppSettings>();
            var highStakeServices = Substitute.For<IHighStakeServices>();
            
            var streamSettings = new StreamSettings { BetMessages = "bet.created" };
            appSettings.StreamSettings.Returns(streamSettings);
            appSettings.SystemType.Returns((string?)null);
            
            var betSubscriber = new BetSubscriber(sentryClient, appSettings, highStakeServices);

            var betCreated = new BetCreated
            {
                GameRoundId = 1,
                Amount = 1000m,
                Currency = "USD"
            };

            var args = CreateMessageArgs(betCreated);

            // Act
            var result = await betSubscriber.OnMessageReceived(null, args);

            // Assert
            result.Should().BeTrue();
            await highStakeServices.Received(1).NotifyHighStake(Arg.Is<BetCreated>(b => 
                b.GameRoundId == betCreated.GameRoundId &&
                b.Amount == betCreated.Amount &&
                b.Currency == betCreated.Currency), string.Empty);
        }

        private static MsgHandlerEventArgs CreateMessageArgs(BetCreated betCreated)
        {
            var jsonMessage = JsonSerializer.Serialize(betCreated);
            var messageData = Encoding.UTF8.GetBytes(jsonMessage);
            var message = new Msg { Data = messageData };
            return new MsgHandlerEventArgs(message);
        }

        private static MsgHandlerEventArgs CreateMessageArgs(string jsonContent)
        {
            var messageData = Encoding.UTF8.GetBytes(jsonContent);
            var message = new Msg { Data = messageData };
            return new MsgHandlerEventArgs(message);
        }
    }
}