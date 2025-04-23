namespace ApiEventConsumer.Service.Tests.Subscribers
{
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using ApiEventConsumer._Shared.Configurations;
    using ApiEventConsumer.Api._Shared.Helpers;
    using ApiEventConsumer.Core.Models.NatMessage;
    using ApiEventConsumer.Service.Service;
    using ApiEventConsumer.Subscribers;
    using Microsoft.Extensions.Configuration;
    using NSubstitute;
    using NATS.Client;
    using Sentry;
    using Xunit;

    public class SubscriberServiceTests
    {
        private readonly ISentryClient _sentryClient;
        private readonly IAppSettings _appSettings;
        private readonly IHighStakeServices _highStakeServices;
        private readonly ConfigurationServices _configDependencies;
        private readonly SubscriberServices _sut;

        public SubscriberServiceTests()
        {
            _sentryClient = Substitute.For<ISentryClient>();
            _appSettings = Substitute.For<IAppSettings>();
            _highStakeServices = Substitute.For<IHighStakeServices>();
            
            _configDependencies = new ConfigurationServices(
                Substitute.For<IConfiguration>(),
                Substitute.For<ICacheManager>(),
                _appSettings,
                Substitute.For<IGameUserService>(),
                Substitute.For<IGeneralGameSettingService>(),
                Substitute.For<IChatBotApi>()
            );

            _sut = new SubscriberServices(
                _sentryClient,
                _highStakeServices,
                _configDependencies
            );
        }

        [Fact]
        public async Task HandleBetCreated_ValidHighStakeBet_NotifiesAndReturnsTrue()
        {
            // Arrange
            var betCreated = new BetCreated { Amount = 5000 };
            var message = JsonSerializer.Serialize(betCreated);
            var msgArgs = new MsgHandlerEventArgs(
                new Msg("subject", Encoding.UTF8.GetBytes(message))
            );

            _appSettings.HighStake.Returns(1000);
            _appSettings.SystemType.Returns("SC1");

            // Act
            var result = await _sut.HandleBetCreated(null, msgArgs);

            // Assert
            Assert.True(result);
            await _highStakeServices.Received(1).NotifyHighStake(
                Arg.Any<BetCreated>(), 
                "SC1"
            );
        }

        [Fact]
        public async Task HandleBetCreated_LowStakeBet_DoesNotNotifyAndReturnsTrue()
        {
            // Arrange
            var betCreated = new BetCreated { Amount = 500 };
            var message = JsonSerializer.Serialize(betCreated);
            var msgArgs = new MsgHandlerEventArgs(
                new Msg("subject", Encoding.UTF8.GetBytes(message))
            );

            _appSettings.HighStake.Returns(1000);
            _appSettings.SystemType.Returns("SC1");

            // Act
            var result = await _sut.HandleBetCreated(null, msgArgs);

            // Assert
            Assert.True(result);
            await _highStakeServices.DidNotReceive().NotifyHighStake(
                Arg.Any<BetCreated>(), 
                Arg.Any<string>()
            );
        }

        [Fact]
        public async Task HandleBetCreated_InvalidMessage_ReturnsFalse()
        {
            // Arrange
            var msgArgs = new MsgHandlerEventArgs(
                new Msg("subject", Encoding.UTF8.GetBytes("invalid json"))
            );

            // Act
            var result = await _sut.HandleBetCreated(null, msgArgs);

            // Assert
            Assert.False(result);
            _sentryClient.Received(1).CaptureException(Arg.Any<Exception>());
        }
    }
}
