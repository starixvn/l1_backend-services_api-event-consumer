using ApiEventConsumer._Shared.Configurations;

namespace ApiEventConsumer.Service.Tests._Shared.Configurations
{
    public class AppSettingsTests
    {
        [Fact]
        public void AppSettings_Properties_ShouldBeSettableAndGettable()
        {
            // Arrange
            var appSettings = new AppSettings();
            var promotionSettings = new PromotionApiSettings();
            var chatBotSettings = new ChatBotApiSettings();
            var databaseSettings = new DatabaseSettings();
            var streamSettings = new StreamSettings();
            var notificationSettings = new NotificationApiSettings();
            var authSettings = new AuthApiSettings();

            // Act
            appSettings.Port = 8080;
            appSettings.SystemType = "TestSystem";
            appSettings.HighStake = 1000.50m;
            appSettings.PromotionApiSettings = promotionSettings;
            appSettings.ChatBotApiSettings = chatBotSettings;
            appSettings.DatabaseSettings = databaseSettings;
            appSettings.StreamSettings = streamSettings;
            appSettings.NotificationApiSettings = notificationSettings;
            appSettings.AuthApiSettings = authSettings;

            // Assert
            Assert.Equal(8080, appSettings.Port);
            Assert.Equal("TestSystem", appSettings.SystemType);
            Assert.Equal(1000.50m, appSettings.HighStake);
            Assert.Same(promotionSettings, appSettings.PromotionApiSettings);
            Assert.Same(chatBotSettings, appSettings.ChatBotApiSettings);
            Assert.Same(databaseSettings, appSettings.DatabaseSettings);
            Assert.Same(streamSettings, appSettings.StreamSettings);
            Assert.Same(notificationSettings, appSettings.NotificationApiSettings);
            Assert.Same(authSettings, appSettings.AuthApiSettings);
        }
    }

    public class ChatBotApiSettingsTests
    {
        [Fact]
        public void ChatBotApiSettings_Properties_ShouldBeSettableAndGettable()
        {
            // Arrange
            var settings = new ChatBotApiSettings();
            var conversationIds = new ConversationIds();

            // Act
            settings.Token = "test-token";
            settings.Url = "https://api.test.com";
            settings.ConversationIds = conversationIds;
            settings.TimeoutInSeconds = 30;
            settings.Proxy = "proxy.test.com";
            settings.HighStakeAmount = 500.75;

            // Assert
            Assert.Equal("test-token", settings.Token);
            Assert.Equal("https://api.test.com", settings.Url);
            Assert.Same(conversationIds, settings.ConversationIds);
            Assert.Equal(30, settings.TimeoutInSeconds);
            Assert.Equal("proxy.test.com", settings.Proxy);
            Assert.Equal(500.75, settings.HighStakeAmount);
        }

        [Fact]
        public void ChatBotApiSettings_DefaultValues_ShouldBeCorrect()
        {
            // Act
            var settings = new ChatBotApiSettings();

            // Assert
            Assert.Null(settings.Token);
            Assert.Null(settings.Url);
            Assert.Null(settings.ConversationIds);
            Assert.Equal(0, settings.TimeoutInSeconds);
            Assert.Null(settings.Proxy);
            Assert.Equal(0.0, settings.HighStakeAmount);
        }
    }

    public class ConversationIdsTests
    {
        [Fact]
        public void ConversationIds_Properties_ShouldBeSettableAndGettable()
        {
            // Arrange
            var conversationIds = new ConversationIds();

            // Act
            conversationIds.HighStake = "highstake-id-123";
            conversationIds.HighMultiplier = "multiplier-id-456";

            // Assert
            Assert.Equal("highstake-id-123", conversationIds.HighStake);
            Assert.Equal("multiplier-id-456", conversationIds.HighMultiplier);
        }

        [Fact]
        public void ConversationIds_DefaultValues_ShouldBeNull()
        {
            // Act
            var conversationIds = new ConversationIds();

            // Assert
            Assert.Null(conversationIds.HighStake);
            Assert.Null(conversationIds.HighMultiplier);
        }
    }

    public class PromotionApiSettingsTests
    {
        [Fact]
        public void PromotionApiSettings_Properties_ShouldBeSettableAndGettable()
        {
            // Arrange
            var settings = new PromotionApiSettings();
            var endpoints = new PromotionEndpoints();

            // Act
            settings.BaseUrl = "https://promotion.api.com";
            settings.Endpoints = endpoints;
            settings.TimeoutInSeconds = 60;

            // Assert
            Assert.Equal("https://promotion.api.com", settings.BaseUrl);
            Assert.Same(endpoints, settings.Endpoints);
            Assert.Equal(60, settings.TimeoutInSeconds);
        }

        [Fact]
        public void PromotionApiSettings_DefaultValues_ShouldBeCorrect()
        {
            // Act
            var settings = new PromotionApiSettings();

            // Assert
            Assert.Null(settings.BaseUrl);
            Assert.Null(settings.Endpoints);
            Assert.Null(settings.TimeoutInSeconds);
        }
    }

    public class PromotionEndpointsTests
    {
        [Fact]
        public void PromotionEndpoints_Properties_ShouldBeSettableAndGettable()
        {
            // Arrange
            var endpoints = new PromotionEndpoints();

            // Act
            endpoints.UpdateTotalTurnover = "/api/update-turnover";
            endpoints.HasBonusWin = "/api/has-bonus-win";
            endpoints.GetRunningPromotions = "/api/running-promotions";

            // Assert
            Assert.Equal("/api/update-turnover", endpoints.UpdateTotalTurnover);
            Assert.Equal("/api/has-bonus-win", endpoints.HasBonusWin);
            Assert.Equal("/api/running-promotions", endpoints.GetRunningPromotions);
        }
    }

    public class DatabaseSettingsTests
    {
        [Fact]
        public void DatabaseSettings_Properties_ShouldBeSettableAndGettable()
        {
            // Arrange
            var settings = new DatabaseSettings();

            // Act
            settings.ConnectionString = "mongodb://localhost:27017";
            settings.DatabaseName = "TestDatabase";
            settings.Collection = "TestCollection";

            // Assert
            Assert.Equal("mongodb://localhost:27017", settings.ConnectionString);
            Assert.Equal("TestDatabase", settings.DatabaseName);
            Assert.Equal("TestCollection", settings.Collection);
        }
    }

    public class StreamSettingsTests
    {
        [Fact]
        public void StreamSettings_Properties_ShouldBeSettableAndGettable()
        {
            // Arrange
            var settings = new StreamSettings();
            var consumerSettings = new ConsumerSettings();

            // Act
            settings.Host = "nats://localhost:4222";
            settings.Enable = true;
            settings.StreamName = "TestStream";
            settings.BetMessages = "bet.created";
            settings.BetSettledMessages = "bet.settled";
            settings.MaxReconnectAttempts = 5;
            settings.ReconnectWaitMs = 1000;
            settings.ShutdownWaitMs = 2000;
            settings.ConsumerSettings = consumerSettings;

            // Assert
            Assert.Equal("nats://localhost:4222", settings.Host);
            Assert.True(settings.Enable);
            Assert.Equal("TestStream", settings.StreamName);
            Assert.Equal("bet.created", settings.BetMessages);
            Assert.Equal("bet.settled", settings.BetSettledMessages);
            Assert.Equal(5, settings.MaxReconnectAttempts);
            Assert.Equal(1000, settings.ReconnectWaitMs);
            Assert.Equal(2000, settings.ShutdownWaitMs);
            Assert.Same(consumerSettings, settings.ConsumerSettings);
        }
    }

    public class ConsumerSettingsTests
    {
        [Fact]
        public void ConsumerSettings_Properties_ShouldBeSettableAndGettable()
        {
            // Arrange
            var settings = new ConsumerSettings();

            // Act
            settings.AckWaitMs = 5000;
            settings.MaxAckPending = 100;
            settings.MaxDeliver = 3;
            settings.PullBatchSize = 10;
            settings.PullTimeoutMs = 1000;
            settings.NoMessageDelayMs = 100;
            settings.ErrorRetryDelayMs = 500;

            // Assert
            Assert.Equal(5000, settings.AckWaitMs);
            Assert.Equal(100, settings.MaxAckPending);
            Assert.Equal(3, settings.MaxDeliver);
            Assert.Equal(10, settings.PullBatchSize);
            Assert.Equal(1000, settings.PullTimeoutMs);
            Assert.Equal(100, settings.NoMessageDelayMs);
            Assert.Equal(500, settings.ErrorRetryDelayMs);
        }

        [Fact]
        public void ConsumerSettings_DefaultValues_ShouldBeZero()
        {
            // Act
            var settings = new ConsumerSettings();

            // Assert
            Assert.Equal(0, settings.AckWaitMs);
            Assert.Equal(0, settings.MaxAckPending);
            Assert.Equal(0, settings.MaxDeliver);
            Assert.Equal(0, settings.PullBatchSize);
            Assert.Equal(0, settings.PullTimeoutMs);
            Assert.Equal(0, settings.NoMessageDelayMs);
            Assert.Equal(0, settings.ErrorRetryDelayMs);
        }
    }

    public class NotificationApiSettingsTests
    {
        [Fact]
        public void NotificationApiSettings_Properties_ShouldBeSettableAndGettable()
        {
            // Arrange
            var settings = new NotificationApiSettings();

            // Act
            settings.BaseUrl = "https://notification.api.com";
            settings.PromotionProgressEndpoint = "/api/promotion-progress";
            settings.TimeoutInSeconds = 45;

            // Assert
            Assert.Equal("https://notification.api.com", settings.BaseUrl);
            Assert.Equal("/api/promotion-progress", settings.PromotionProgressEndpoint);
            Assert.Equal(45, settings.TimeoutInSeconds);
        }

        [Fact]
        public void NotificationApiSettings_DefaultValues_ShouldBeCorrect()
        {
            // Act
            var settings = new NotificationApiSettings();

            // Assert
            Assert.Null(settings.BaseUrl);
            Assert.Null(settings.PromotionProgressEndpoint);
            Assert.Equal(0, settings.TimeoutInSeconds);
        }
    }

    public class AuthApiSettingsTests
    {
        [Fact]
        public void AuthApiSettings_Properties_ShouldBeSettableAndGettable()
        {
            // Arrange
            var settings = new AuthApiSettings();

            // Act
            settings.BaseUrl = "https://auth.api.com";
            settings.TokenEndpoint = "/oauth/token";
            settings.ClientId = "test-client-id";
            settings.ClientSecret = "test-client-secret";
            settings.Scope = "read write";
            settings.TimeoutInSeconds = 30;

            // Assert
            Assert.Equal("https://auth.api.com", settings.BaseUrl);
            Assert.Equal("/oauth/token", settings.TokenEndpoint);
            Assert.Equal("test-client-id", settings.ClientId);
            Assert.Equal("test-client-secret", settings.ClientSecret);
            Assert.Equal("read write", settings.Scope);
            Assert.Equal(30, settings.TimeoutInSeconds);
        }

        [Fact]
        public void AuthApiSettings_DefaultValues_ShouldBeCorrect()
        {
            // Act
            var settings = new AuthApiSettings();

            // Assert
            Assert.Null(settings.BaseUrl);
            Assert.Null(settings.TokenEndpoint);
            Assert.Null(settings.ClientId);
            Assert.Null(settings.ClientSecret);
            Assert.Null(settings.Scope);
            Assert.Equal(0, settings.TimeoutInSeconds);
        }
    }

    public class IAppSettingsTests
    {
        private class TestAppSettings : IAppSettings
        {
            public int Port { get; set; }
            public string? SystemType { get; set; }
            public decimal HighStake { get; set; }
            public PromotionApiSettings? PromotionApiSettings { get; set; }
            public ChatBotApiSettings? ChatBotApiSettings { get; set; }
            public DatabaseSettings DatabaseSettings { get; set; } = null!;
            public StreamSettings StreamSettings { get; set; } = null!;
            public NotificationApiSettings? NotificationApiSettings { get; set; }
            public AuthApiSettings? AuthApiSettings { get; set; }
        }

        [Fact]
        public void IAppSettings_Implementation_ShouldWorkCorrectly()
        {
            // Arrange
            IAppSettings settings = new TestAppSettings();

            // Act
            settings.Port = 9000;
            settings.SystemType = "TestInterface";
            settings.HighStake = 2500.00m;

            // Assert
            Assert.Equal(9000, settings.Port);
            Assert.Equal("TestInterface", settings.SystemType);
            Assert.Equal(2500.00m, settings.HighStake);
        }
    }
}
