using FluentAssertions;
using ApiEventConsumer.Services;
using ApiEventConsumer.Database;
using ApiEventConsumer.Database.Entities;
using NSubstitute;

namespace ApiEventConsumer.Service.Tests.Service
{
    public class GeneralGameSettingServiceTests
    {
        private readonly IGenericRepository<GameConfigEntity> _repository;

        public GeneralGameSettingServiceTests()
        {
            _repository = Substitute.For<IGenericRepository<GameConfigEntity>>();
        }

        [Fact]
        public async Task GetDisplayName_WhenDisplayNameExists_ShouldReturnDisplayName()
        {
            // Arrange
            const int gameId = 1;
            const string expectedDisplayName = "Test Game";

            SetupLocalGameConfig(gameId, displayName: expectedDisplayName);
            var service = CreateService();

            // Act
            var result = await service.GetDisplayName(gameId);

            // Assert
            result.Should().Be(expectedDisplayName);
        }

        [Fact]
        public async Task GetDisplayName_WhenNoNameExists_ShouldReturnEmpty()
        {
            // Arrange
            const int gameId = 1;

            SetupLocalGameConfig(gameId);
            var service = CreateService();

            // Act
            var result = await service.GetDisplayName(gameId);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetDisplayName_WithMultipleGamesInDb_ShouldReturnCorrectGame()
        {
            // Arrange
            const int targetGameId = 2;
            var configs = new[]
            {
                new GameConfigEntity { GameId = 1, DisplayName = "Game 1" },
                new GameConfigEntity { GameId = 2, DisplayName = "Game 2" },
                new GameConfigEntity { GameId = 3, DisplayName = "Game 3" }
            }.AsQueryable();

            _repository.AsQueryable().Returns(configs);

            var service = CreateService();

            // Act
            var result = await service.GetDisplayName(targetGameId);

            // Assert
            result.Should().Be("Game 2");
        }

        [Fact]
        public async Task GetDisplayName_WhenDisplayNameAndNameBothExist_ShouldPreferDisplayName()
        {
            // Arrange
            const int gameId = 1;
            const string displayName = "Custom Display Name";
            const string name = "KenoMatch1";

            SetupLocalGameConfig(gameId, displayName: displayName, name: name);
            var service = CreateService();

            // Act
            var result = await service.GetDisplayName(gameId);

            // Assert
            result.Should().Be(displayName);
        }

        [Fact]
        public async Task GetDisplayName_WhenOnlyNameExists_ShouldUseGameEnumDisplayName()
        {
            // Arrange
            const int gameId = 1;
            const string name = "Keno Soccer";

            SetupLocalGameConfig(gameId, displayName: null, name: name);
            var service = CreateService();

            // Act
            var result = await service.GetDisplayName(gameId);

            // Assert
            result.Should().Be("Keno Soccer"); // Assuming this is the DisplayName from Game enum
        }

        [Fact]
        public async Task GetDisplayName_WhenBothNamesAreEmpty_ShouldReturnEmptyString()
        {
            // Arrange
            const int gameId = 1;

            SetupLocalGameConfig(gameId, displayName: null, name: null);
            var service = CreateService();

            // Act
            var result = await service.GetDisplayName(gameId);

            // Assert
            result.Should().BeEmpty();
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task GetDisplayName_WhenDisplayNameIsNullOrEmpty_ShouldFallbackToGameName(string? displayName)
        {
            // Arrange
            const int gameId = 1;
            var configs = new[]
            {
                new GameConfigEntity
                {
                    GameId = gameId,
                    DisplayName = displayName,
                    Name = "GameName"
                }
            }.AsQueryable();

            _repository.AsQueryable().Returns(configs);
            var service = CreateService();

            // Act
            var result = await service.GetDisplayName(gameId);

            // Assert
            result.Should().Be("GameName");
        }

        private GeneralGameSettingServices CreateService()
        {
            return new GeneralGameSettingServices(_repository);
        }

        private void SetupLocalGameConfig(int gameId, string? displayName = null, string? name = null)
        {
            var configs = new[]
            {
                new GameConfigEntity
                {
                    GameId = gameId,
                    DisplayName = displayName,
                    Name = name
                }
            }.AsQueryable();

            _repository.AsQueryable().Returns(configs);
        }
    }

    public class ApiResponse<T>
    {
        public T? Data { get; set; }
    }
}