using System.Text;
using System.Text.Json;
using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Api._Shared.Helpers;
using ApiEventConsumer.Core.Models.ChatBot;
using ApiEventConsumer.Core.Models.NatMessage;
using ApiEventConsumer.Core.Models.Promotion;
using ApiEventConsumer.Services;
using ApiEventConsumer.Subscribers;
using ApiEventConsumer.Subscribers.Bet;
using Microsoft.Extensions.Logging;
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
        private readonly IChatBotApi chatBotApi;
        private readonly IGeneralGameSettingService generalGameSettingService;
        private readonly ISharedCacheService sharedCache;
        private readonly BetSettledSubscriber sut;

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
            generalGameSettingService = Substitute.For<IGeneralGameSettingService>();
            sharedCache = Substitute.For<ISharedCacheService>();

            appSettings.PromotionApiSettings.Returns(promotionApiSettings);

            // Setup default PromotionService mock response for all tests
            var defaultResponse = Task.FromResult(new UpdateTotalTurnoverResponse { Success = true, ErrorMessage = "Success" });
            promotionService.UpdateTotalTurnoverAsync(Arg.Any<UpdateTotalTurnoverRequest>()).Returns(defaultResponse);

            // Setup default ChatBot API response
            var chatBotResponse = Task.FromResult(new ChatBotResult { Success = true });
            chatBotApi.ForwardMessage(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(chatBotResponse);

            // Setup default ChatBot settings for high multiplier tests
            var chatBotSettings = new ChatBotApiSettings
            {
                Token = "test-token",
                ConversationIds = new ConversationIds
                {
                    HighMultiplier = "high-multiplier-conversation-id"
                }
            };
            appSettings.ChatBotApiSettings.Returns(chatBotSettings);

            sut = new BetSettledSubscriber(sentryClient, appSettings, promotionService, notificationService, chatBotApi, generalGameSettingService, sharedCache);
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

            // Setup cache to return eligible promotions
            var eligiblePromotions = new List<Promotion>
            {
                new Promotion
                {
                    Name = "Test Promotion",
                    OperatorId = "op_456",
                    StartDate = DateTime.UtcNow.AddDays(-1),
                    ClaimExpirationDate = DateTime.UtcNow.AddDays(1),
                    GameIds = [2], // Match the default gameTypeId
                    OfferedSiteNames = [], // Empty means all sites
                    OfferedPlayerIds = [] // Empty means all players
                }
            };
            sharedCache.Get<List<Promotion>>(Arg.Any<string>()).Returns(eligiblePromotions);

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

        #region High Multiplier Tests

        [Fact]
        public async Task OnMessageReceived_WithHighMultiplier_ShouldIncludeGameNameInMessage()
        {
            // Arrange
            const int gameTypeId = 5;
            const string expectedGameName = "Keno Soccer";
            var betSettled = CreateBetSettledMessage(
                gameTypeId: gameTypeId,
                totalAmount: 100m,
                totalWin: 15000m // 15x multiplier (above 10x threshold)
            );
            var args = CreateMessageArgs(betSettled);

            generalGameSettingService.GetDisplayName(gameTypeId).Returns(Task.FromResult(expectedGameName));

            // Act
            var result = await sut.OnMessageReceived(this, args);

            // Assert
            Assert.True(result);
            await generalGameSettingService.Received(1).GetDisplayName(gameTypeId);
            await chatBotApi.Received(1).ForwardMessage(
                "test-token",
                "high-multiplier-conversation-id",
                Arg.Is<string>(msg => msg.Contains($"GameName={expectedGameName}")));
        }

        [Fact]
        public async Task OnMessageReceived_WithHighMultiplier_ShouldFormatMessageCorrectly()
        {
            // Arrange
            const int gameTypeId = 3;
            const int playerId = 12345;
            const int gameRoundId = 67890;
            const string gameName = "Test Game";
            const decimal totalAmount = 50m;
            const decimal totalWin = 75000m; // 15x multiplier

            var betSettled = CreateBetSettledMessage(
                gameTypeId: gameTypeId,
                gameRoundId: gameRoundId,
                playerId: playerId,
                totalAmount: totalAmount,
                totalWin: totalWin
            );
            var args = CreateMessageArgs(betSettled);

            generalGameSettingService.GetDisplayName(gameTypeId).Returns(Task.FromResult(gameName));

            // Act
            var result = await sut.OnMessageReceived(this, args);

            // Assert
            Assert.True(result);
            await chatBotApi.Received(1).ForwardMessage(
                "test-token",
                "high-multiplier-conversation-id",
                Arg.Is<string>(msg =>
                    msg.Contains($"PlayerId={playerId}") &&
                    msg.Contains($"GameTypeId={gameTypeId}") &&
                    msg.Contains($"GameName={gameName}") &&
                    msg.Contains($"GameRoundId={gameRoundId}") &&
                    msg.Contains($"TotalWin={totalWin}") &&
                    msg.Contains("Multiplier=1500.00x")));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task OnMessageReceived_WithHighMultiplierAndEmptyGameName_ShouldStillSendMessage(string? emptyGameName)
        {
            // Arrange
            const int gameTypeId = 7;
            var betSettled = CreateBetSettledMessage(
                gameTypeId: gameTypeId,
                totalAmount: 100m,
                totalWin: 12000m // 12x multiplier
            );
            var args = CreateMessageArgs(betSettled);

            generalGameSettingService.GetDisplayName(gameTypeId).Returns(Task.FromResult(emptyGameName ?? string.Empty));

            // Act
            var result = await sut.OnMessageReceived(this, args);

            // Assert
            Assert.True(result);
            await generalGameSettingService.Received(1).GetDisplayName(gameTypeId);
            await chatBotApi.Received(1).ForwardMessage(
                "test-token", 
                "high-multiplier-conversation-id", 
                Arg.Any<string>());
        }

        [Theory]
        [InlineData(100, 999)] // Just under 10x threshold (9.99x)
        [InlineData(100, 1000)] // Exactly 10x threshold (should NOT trigger)
        [InlineData(50, 450)] // 9x multiplier
        public async Task OnMessageReceived_WithLowMultiplier_ShouldNotSendChatBotMessage(decimal totalAmount, decimal totalWin)
        {
            // Arrange
            var betSettled = CreateBetSettledMessage(
                totalAmount: totalAmount,
                totalWin: totalWin
            );
            var args = CreateMessageArgs(betSettled);

            // Act
            var result = await sut.OnMessageReceived(this, args);

            // Assert
            Assert.True(result);
            await generalGameSettingService.DidNotReceive().GetDisplayName(Arg.Any<int>());
            await chatBotApi.DidNotReceive().ForwardMessage(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        }

        [Fact]
        public async Task OnMessageReceived_WithHighMultiplierExactlyAtThreshold_ShouldNotSendMessage()
        {
            // Arrange - 10.0x multiplier exactly at threshold
            var betSettled = CreateBetSettledMessage(
                totalAmount: 100m,
                totalWin: 1000m
            );
            var args = CreateMessageArgs(betSettled);

            // Act
            var result = await sut.OnMessageReceived(this, args);

            // Assert
            Assert.True(result);
            await generalGameSettingService.DidNotReceive().GetDisplayName(Arg.Any<int>());
            await chatBotApi.DidNotReceive().ForwardMessage(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        }

        [Fact]
        public async Task OnMessageReceived_WithHighMultiplierJustAboveThreshold_ShouldSendMessage()
        {
            // Arrange - 10.01x multiplier just above threshold
            const string gameName = "Lucky Game";
            var betSettled = CreateBetSettledMessage(
                gameTypeId: 8,
                totalAmount: 100m,
                totalWin: 1000001m // 10.01x multiplier
            );
            var args = CreateMessageArgs(betSettled);

            generalGameSettingService.GetDisplayName(8).Returns(Task.FromResult(gameName));

            // Act
            var result = await sut.OnMessageReceived(this, args);

            // Assert
            Assert.True(result);
            await generalGameSettingService.Received(1).GetDisplayName(8);
            await chatBotApi.Received(1).ForwardMessage(
                "test-token",
                "high-multiplier-conversation-id",
                Arg.Is<string>(msg => msg.Contains("GameName=Lucky Game") && msg.Contains("Multiplier=10000.01x")));
        }

        [Fact]
        public async Task OnMessageReceived_WithZeroTotalAmount_ShouldNotSendMessage()
        {
            // Arrange - Zero total amount should prevent division and message sending
            var betSettled = CreateBetSettledMessage(
                totalAmount: 0m,
                totalWin: 1500m
            );
            var args = CreateMessageArgs(betSettled);

            // Act
            var result = await sut.OnMessageReceived(this, args);

            // Assert
            Assert.True(result);
            await generalGameSettingService.DidNotReceive().GetDisplayName(Arg.Any<int>());
            await chatBotApi.DidNotReceive().ForwardMessage(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        }

        [Fact]
        public async Task OnMessageReceived_WithGeneralGameSettingServiceException_ShouldContinueProcessing()
        {
            // Arrange
            var betSettled = CreateBetSettledMessage(
                gameTypeId: 9,
                totalAmount: 100m,
                totalWin: 20000m // 20x multiplier
            );
            var args = CreateMessageArgs(betSettled);

            generalGameSettingService.GetDisplayName(9).Returns(Task.FromException<string>(new Exception("Service unavailable")));

            // Act
            var result = await sut.OnMessageReceived(this, args);

            // Assert - Should still return true and handle the exception gracefully
            Assert.True(result);
            await generalGameSettingService.Received(1).GetDisplayName(9);
            // ChatBot message should not be sent due to exception
            await chatBotApi.DidNotReceive().ForwardMessage(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        }

        [Fact]
        public async Task OnMessageReceived_WithDifferentCaseSiteName_ShouldProcessSuccessfully()
        {
            // Arrange - Test site name case-insensitive matching
            var betSettled = CreateBetSettledMessage(
                operatorId: "test-op",
                siteName: "TestSite.COM"  // Mixed case in message
            );
            var args = CreateMessageArgs(betSettled);

            // Setup promotion with different case site name
            var promotions = new List<Promotion>
            {
                new Promotion
                {
                    Name = "Test Promotion",
                    OperatorId = "test-op",
                    StartDate = DateTime.UtcNow.AddDays(-1),
                    ClaimExpirationDate = DateTime.UtcNow.AddDays(1),
                    GameIds = [2], 
                    OfferedSiteNames = ["testsite.com"], // Different case in promotion
                    OfferedPlayerIds = []
                }
            };
            sharedCache.Get<List<Promotion>>(Arg.Any<string>()).Returns(promotions);

            // Act
            var result = await sut.OnMessageReceived(this, args);

            // Assert - Should process despite case difference  
            Assert.True(result);
            await promotionService.Received(1).UpdateTotalTurnoverAsync(Arg.Any<UpdateTotalTurnoverRequest>());
        }

        #endregion

        #region Helper Methods for High Multiplier Tests

        private static BetSettled CreateHighMultiplierBetSettled(int gameTypeId = 1, decimal totalAmount = 100m, decimal totalWin = 1500m)
        {
            return CreateBetSettledMessage(
                gameTypeId: gameTypeId,
                totalAmount: totalAmount,
                totalWin: totalWin
            );
        }

        #endregion
    }
}