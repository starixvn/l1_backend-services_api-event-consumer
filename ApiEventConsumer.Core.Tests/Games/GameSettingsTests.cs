using ApiEventConsumer.Core.Games;

namespace ApiEventConsumer.Core.Tests.Games
{
    public class GameSettingsTests
    {
        [Fact]
        public void GameSettings_Properties_ShouldSetAndGetCorrectly()
        {
            // Arrange
            var gameSettings = new GameSettings
            {
                GameId = 1,
                GameName = "TestGame",
                DisplayName = "Test Game",
                BetChoices = new[] { "Bet1", "Bet2" },
                Odds = new[] { "1.5", "2.0" },
                OfferedSystem = new[] { "System1", "System2" }
            };

            // Assert
            Assert.Equal(1, gameSettings.GameId);
            Assert.Equal("TestGame", gameSettings.GameName);
            Assert.Equal("Test Game", gameSettings.DisplayName);
            Assert.Equal(new[] { "Bet1", "Bet2" }, gameSettings.BetChoices);
            Assert.Equal(new[] { "1.5", "2.0" }, gameSettings.Odds);
            Assert.Equal(new[] { "System1", "System2" }, gameSettings.OfferedSystem);
        }

        [Fact]
        public void GamesSettings_GetGamesSettings_ShouldReturnSingleInstance()
        {
            // Act
            var instance1 = GamesSettings.GetGamesSettings();
            var instance2 = GamesSettings.GetGamesSettings();

            // Assert
            Assert.NotNull(instance1);
            Assert.NotNull(instance2);
            Assert.Same(instance1, instance2); // Verify singleton pattern
        }

        [Fact]
        public void GamesSettings_GetGameFromName_WithNullGames_ShouldReturnNull()
        {
            // Arrange
            var gamesSettings = GamesSettings.GetGamesSettings();

            // Act
            var result = gamesSettings.GetGameFromName("NonExistentGame");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GamesSettings_GetGameFromId_WithNullGames_ShouldReturnNull()
        {
            // Arrange
            var gamesSettings = GamesSettings.GetGamesSettings();

            // Act
            var result = gamesSettings.GetGameFromId(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GameSettings_Properties_ShouldAllowNullValues()
        {
            // Arrange
            var gameSettings = new GameSettings();

            // Assert
            Assert.Equal(0, gameSettings.GameId); // Default value for int
            Assert.Null(gameSettings.GameName);
            Assert.Null(gameSettings.DisplayName);
            Assert.Null(gameSettings.BetChoices);
            Assert.Null(gameSettings.Odds);
            Assert.Null(gameSettings.OfferedSystem);
        }

        [Fact]
        public void GamesSettings_Games_ShouldBeAccessible()
        {
            // Arrange
            var gamesSettings = GamesSettings.GetGamesSettings();

            // Act
            var games = gamesSettings.Games;

            // Assert
            Assert.Null(games); // Since we haven't initialized any games
        }

        [Theory]
        [InlineData("")]
        [InlineData("NonExistentGame")]
        public void GamesSettings_GetGameFromName_WithInvalidName_ShouldReturnNull(string gameName)
        {
            // Arrange
            var gamesSettings = GamesSettings.GetGamesSettings();

            // Act
            var result = gamesSettings.GetGameFromName(gameName);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(999)]
        public void GamesSettings_GetGameFromId_WithInvalidId_ShouldReturnNull(int gameId)
        {
            // Arrange
            var gamesSettings = GamesSettings.GetGamesSettings();

            // Act
            var result = gamesSettings.GetGameFromId(gameId);

            // Assert
            Assert.Null(result);
        }
    }
}
