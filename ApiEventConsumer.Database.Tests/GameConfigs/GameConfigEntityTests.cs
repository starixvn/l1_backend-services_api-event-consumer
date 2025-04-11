using ApiEventConsumer.Database.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ApiEventConsumer.Database.Tests.GameConfigs
{
    public class GameConfigEntityTests
    {
        [Fact]
        public void GameConfigEntity_ShouldHaveCorrectCollectionName()
        {
            // Act
            var collectionName = Document.GetCollectionName(typeof(GameConfigEntity));

            // Assert
            Assert.Equal("game_configs", collectionName);
        }

        [Fact]
        public void GameConfigEntity_Properties_ShouldSetAndGetCorrectly()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var gameConfig = new GameConfigEntity
            {
                Id = ObjectId.GenerateNewId(),
                Name = "poker",
                DisplayName = "Texas Poker",
                GameId = 123,
                CreatedAt = now,
                UpdatedAt = now.AddHours(1)
            };

            // Assert
            Assert.NotEqual(ObjectId.Empty, gameConfig.Id);
            Assert.Equal("poker", gameConfig.Name);
            Assert.Equal("Texas Poker", gameConfig.DisplayName);
            Assert.Equal(123, gameConfig.GameId);
            Assert.Equal(now, gameConfig.CreatedAt);
            Assert.Equal(now.AddHours(1), gameConfig.UpdatedAt);
        }

        [Fact]
        public void GameConfigEntity_DefaultValues_ShouldBeCorrect()
        {
            // Arrange
            var gameConfig = new GameConfigEntity();

            // Assert
            Assert.Equal(ObjectId.Empty, gameConfig.Id);
            Assert.Null(gameConfig.Name);
            Assert.Null(gameConfig.DisplayName);
            Assert.Equal(0, gameConfig.GameId);
            Assert.Equal(default(DateTime), gameConfig.CreatedAt);
            Assert.Null(gameConfig.UpdatedAt);
        }

        [Theory]
        [InlineData("poker", "Texas Poker", 1)]
        [InlineData("blackjack", "Black Jack", 2)]
        [InlineData("roulette", "Roulette", 3)]
        public void GameConfigEntity_ShouldHandleVariousGameTypes(string name, string displayName, int gameId)
        {
            // Arrange
            var gameConfig = new GameConfigEntity
            {
                Name = name,
                DisplayName = displayName,
                GameId = gameId
            };

            // Assert
            Assert.Equal(name, gameConfig.Name);
            Assert.Equal(displayName, gameConfig.DisplayName);
            Assert.Equal(gameId, gameConfig.GameId);
        }

        [Theory]
        [InlineData(null, null, 0)]
        [InlineData("", "", 1)]
        [InlineData(" ", " ", -1)]
        public void GameConfigEntity_ShouldHandleEdgeCases(string? name, string? displayName, int gameId)
        {
            // Arrange
            var gameConfig = new GameConfigEntity
            {
                Name = name,
                DisplayName = displayName,
                GameId = gameId
            };

            // Assert
            Assert.Equal(name, gameConfig.Name);
            Assert.Equal(displayName, gameConfig.DisplayName);
            Assert.Equal(gameId, gameConfig.GameId);
        }

        [Fact]
        public void GameConfigEntity_BsonElementAttributes_ShouldBeCorrect()
        {
            // Arrange & Act
            var type = typeof(GameConfigEntity);
            var nameProperty = type.GetProperty("Name");
            var displayNameProperty = type.GetProperty("DisplayName");
            var gameIdProperty = type.GetProperty("GameId");

            // Assert
            Assert.Equal("name", GetBsonElementValue(nameProperty));
            Assert.Equal("displayName", GetBsonElementValue(displayNameProperty));
            Assert.Equal("gameId", GetBsonElementValue(gameIdProperty));
        }

        [Fact]
        public void GameConfigEntity_ShouldInheritFromDocument()
        {
            // Arrange
            var gameConfig = new GameConfigEntity();

            // Assert
            Assert.IsAssignableFrom<Document>(gameConfig);
        }

        [Fact]
        public void GameConfigEntity_TimestampsShouldBeSettable()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var gameConfig = new GameConfigEntity
            {
                CreatedAt = now,
                UpdatedAt = now.AddMinutes(5)
            };

            // Assert
            Assert.Equal(now, gameConfig.CreatedAt);
            Assert.Equal(now.AddMinutes(5), gameConfig.UpdatedAt);
        }

        private string GetBsonElementValue(System.Reflection.PropertyInfo? property)
        {
            var attribute = property?.GetCustomAttributes(typeof(BsonElementAttribute), true)
                .FirstOrDefault() as BsonElementAttribute;
            return attribute?.ElementName ?? string.Empty;
        }
    }
}
