using System.Text;
using System.Text.Json;
using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Core.Models.NatMessage;
using ApiEventConsumer.Core.Models.Promotion;
using ApiEventConsumer.Services;
using ApiEventConsumer.Subscribers;
using ApiEventConsumer.Subscribers.Bet;
using NATS.Client;
using NSubstitute;

namespace ApiEventConsumer.Service.Tests.Subscribers
{
    public class BetSettledSubscriberTests
    {
        private readonly PromotionApiSettings promotionApiSettings;
        private readonly ISentryClient sentryClient;
        private readonly IAppSettings appSettings;
        private readonly IPromotionService promotionService;
        private readonly INotificationService notificationService;
        private readonly BetSettledSubscriber sut;

        private readonly IChatBotApi chatBotApi;

        public BetSettledSubscriberTests()
        {
            promotionApiSettings = new PromotionApiSettings
            {
                BaseUrl = "https://test-api.com",
                Endpoints = new PromotionEndpoints
                {
                    UpdateTotalTurnover = "/bonus-wins/update-total-turnover",
                    HasBonusWin = "/bonus-wins/has-bonus-win"
                }
            };

            sentryClient = Substitute.For<ISentryClient>();
            appSettings = Substitute.For<IAppSettings>();
            promotionService = Substitute.For<IPromotionService>();
            notificationService = Substitute.For<INotificationService>();
            chatBotApi = Substitute.For<IChatBotApi>();

            appSettings.PromotionApiSettings.Returns(promotionApiSettings);

            // Setup default PromotionService mock response for all tests
            var defaultResponse = Task.FromResult(new UpdateTotalTurnoverResponse { Success = true, ErrorMessage = "Success" });
            promotionService.UpdateTotalTurnoverAsync(Arg.Any<UpdateTotalTurnoverRequest>()).Returns(defaultResponse);

            sut = new BetSettledSubscriber(sentryClient, appSettings, promotionService, notificationService, chatBotApi);
        }

        private static BetSettled CreateBetSettledMessage(
            int gameRoundId = 123,
            int gameTypeId = 2,
            decimal totalAmount = 100.00m,
            decimal totalWin = 150.00m,
            decimal validBetAmount = 95.00m,
            bool freeBet = false,
            string? promotionId = null,
            string seq = "seq_123",
            object? playerId = null,
            string siteId = "site_123",
            string operatorId = "op_123",
            string siteName = "site_name_123")
        {
            return new BetSettled
            {
                GameRoundId = gameRoundId,
                GameTypeId = gameTypeId,
                TotalAmount = totalAmount,
                TotalWin = totalWin,
                ValidBetAmount = validBetAmount,
                FreeBet = freeBet,
                PromotionId = promotionId ?? string.Empty,
                Seq = seq,
                PlayerId = playerId ?? 456,
                SiteId = siteId,
                OperatorId = operatorId,
                SiteName = siteName
            };
        }

        private static MsgHandlerEventArgs CreateMessageArgs(BetSettled betSettled)
        {
            var jsonMessage = JsonSerializer.Serialize(betSettled);
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

        [Theory]
        [InlineData("test.bet.settled", "test.bet.settled")]
        [InlineData("vg.local.api.bet.settled", "vg.local.api.bet.settled")]
        [InlineData("", "")]
        public void SubjectName_ShouldReturnBetSettledMessagesFromAppSettings(string betSettledMessages, string expected)
        {
            // Arrange
            var streamSettings = new StreamSettings { BetSettledMessages = betSettledMessages };
            appSettings.StreamSettings.Returns(streamSettings);

            // Act
            var result = sut.SubjectName;

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void SubjectName_WhenStreamSettingsIsNull_ShouldReturnEmptyString()
        {
            // Arrange
            appSettings.StreamSettings.Returns((StreamSettings?)null);

            // Act
            var result = sut.SubjectName;

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void SubjectName_WhenBetSettledMessagesIsNull_ShouldReturnEmptyString()
        {
            // Arrange
            var streamSettings = new StreamSettings { BetSettledMessages = null };
            appSettings.StreamSettings.Returns(streamSettings);

            // Act
            var result = sut.SubjectName;

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void Constructor_ShouldInitializeCorrectly()
        {
            // Assert
            Assert.NotNull(sut);
        }

        [Fact]
        public async Task OnMessageReceived_WithValidBetSettledMessage_ShouldProcessSuccessfully()
        {
            // Arrange
            var betSettled = CreateBetSettledMessage();
            var args = CreateMessageArgs(betSettled);

            // Act
            var result = await sut.OnMessageReceived(this, args);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task OnMessageReceived_WithNullBetSettled_ShouldReturnTrue()
        {
            // Arrange
            var args = CreateMessageArgs("null");

            // Act
            var result = await sut.OnMessageReceived(this, args);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task OnMessageReceived_WithPromotionalTicket_ShouldCallPromotionServiceWithCorrectParameters()
        {
            // Arrange
            var betSettled = CreateBetSettledMessage(
                gameRoundId: 456,
                promotionId: "promo_123",
                freeBet: true,
                totalAmount: 100.50m,
                totalWin: 200.00m,
                validBetAmount: 95.75m,
                playerId: 789,
                operatorId: "op_456",
                siteName: "test_site"
            );
            var args = CreateMessageArgs(betSettled);

            // Act
            var result = await sut.OnMessageReceived(this, args);

            // Assert
            Assert.True(result);
            await promotionService.Received(1).UpdateTotalTurnoverAsync(Arg.Is<UpdateTotalTurnoverRequest>(r =>
                r.PlayerId == "789" &&
                r.OperatorId == "op_456" &&
                r.SiteName == "test_site" &&
                r.BetAmount == 95.75m));
        }
    }
}