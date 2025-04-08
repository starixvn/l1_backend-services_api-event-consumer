using Xunit;
using Moq;
using FluentAssertions;
using ApiEventConsumer.Service.Service;
using ApiEventConsumer._Shared.Configurations;
using ApiEventConsumer.Database;
using ApiEventConsumer.Database.Entities;
using ApiEventConsumer.Core.Enumeration;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System;

namespace ApiEventConsumer.Service.Tests.Service
{
    public class GeneralGameSettingServiceTests
    {
        private readonly Mock<IGenericRepository<GameConfigEntity>> _mockRepository;
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<IAppSettings> _mockAppSettings;
        private readonly Mock<CloudSettings> _mockCloudSettings;
        private readonly Mock<IGameManagementApi> _mockGameManagementApi;

        public GeneralGameSettingServiceTests()
        {
            _mockRepository = new Mock<IGenericRepository<GameConfigEntity>>();
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockAppSettings = new Mock<IAppSettings>();
            _mockCloudSettings = new Mock<CloudSettings>();
            _mockGameManagementApi = new Mock<IGameManagementApi>();

            _mockAppSettings.Setup(x => x.CloudSettings).Returns(_mockCloudSettings.Object);
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

        private GeneralGameSettingService CreateService()
        {
            return new GeneralGameSettingService(
                _mockRepository.Object,
                _mockServiceProvider.Object,
                _mockAppSettings.Object
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

        private void SetupEmptyRepository()
        {
            _mockRepository.Setup(x => x.AsQueryable())
                          .Returns(Enumerable.Empty<GameConfigEntity>().AsQueryable());
        }
    }

    // Helper class to match the API response structure
    public class ApiResponse<T>
    {
        public T Data { get; set; }
    }
}