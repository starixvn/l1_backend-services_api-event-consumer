using Moq;
using FluentAssertions;
using ApiEventConsumer.Service.Service;
using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Database;
using ApiEventConsumer.Database.Entities;

namespace ApiEventConsumer.Service.Tests.Service
{
    public class GeneralGameSettingServiceTests
    {
        private readonly Mock<IGenericRepository<GameConfigEntity>> _mockRepository;

        public GeneralGameSettingServiceTests()
        {
            _mockRepository = new Mock<IGenericRepository<GameConfigEntity>>();

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

            _mockRepository.Setup(x => x.AsQueryable())
                          .Returns(configs);

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
        [InlineData("   ")]
        public async Task GetDisplayName_WhenDisplayNameIsNullOrEmpty_ShouldFallbackToGameName(string displayName)
        {
            // Arrange
            const int gameId = 1;
            var configs = new[]
            {
                new GameConfigEntity
                {
                    GameId = gameId,
                    DisplayName = displayName
                }
            }.AsQueryable();

            _mockRepository.Setup(x => x.AsQueryable()).Returns(configs);
        }


        private GeneralGameSettingService CreateService()
        {
            return new GeneralGameSettingService(
                _mockRepository.Object
            );
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

            _mockRepository.Setup(x => x.AsQueryable()).Returns(configs);
        }
    }

    // Helper class to match the API response structure
    public class ApiResponse<T>
    {
        public T? Data { get; set; }
    }
}