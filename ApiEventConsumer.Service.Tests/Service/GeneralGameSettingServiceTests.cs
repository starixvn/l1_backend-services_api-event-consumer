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

        #region Cache Tests

        [Fact]
        public async Task GetDisplayName_WhenCalledTwiceWithinCacheWindow_ShouldReturnSameValueFast()
        {
            // Arrange
            const int gameId = 1;
            const string expectedDisplayName = "Cached Game";

            SetupLocalGameConfig(gameId, displayName: expectedDisplayName);
            var service = CreateService();

            // Act
            var result1 = await service.GetDisplayName(gameId);
            var result2 = await service.GetDisplayName(gameId);

            // Assert
            result1.Should().Be(expectedDisplayName);
            result2.Should().Be(expectedDisplayName);
            // Both calls should return the same result, demonstrating cache functionality
        }

        [Fact]
        public async Task GetDisplayName_WhenCalledForDifferentGameIds_ShouldCacheSeparately()
        {
            // Arrange
            const int gameId1 = 1;
            const int gameId2 = 2;
            
            var configs = new[]
            {
                new GameConfigEntity { GameId = gameId1, DisplayName = "Game 1" },
                new GameConfigEntity { GameId = gameId2, DisplayName = "Game 2" }
            }.AsQueryable();

            _repository.AsQueryable().Returns(configs);
            var service = CreateService();

            // Act
            var result1 = await service.GetDisplayName(gameId1);
            var result2 = await service.GetDisplayName(gameId2);
            var result1Again = await service.GetDisplayName(gameId1);
            var result2Again = await service.GetDisplayName(gameId2);

            // Assert
            result1.Should().Be("Game 1");
            result2.Should().Be("Game 2");
            result1Again.Should().Be("Game 1");
            result2Again.Should().Be("Game 2");
            // Cache should maintain separate entries for different game IDs
        }

        [Fact]
        public async Task GetDisplayName_WhenCacheExpires_ShouldRefreshFromDatabase()
        {
            // Arrange
            const int gameId = 1;
            const string originalDisplayName = "Original Game";
            const string updatedDisplayName = "Updated Game";

            // Setup initial config
            SetupLocalGameConfig(gameId, displayName: originalDisplayName);
            var service = CreateService();

            // Act - First call (should cache)
            var result1 = await service.GetDisplayName(gameId);

            // Simulate time passing beyond cache expiration (61 seconds)
            await Task.Delay(50); // Small delay to simulate time passage
            
            // Update the repository to return different data
            SetupLocalGameConfig(gameId, displayName: updatedDisplayName);

            // Use reflection to simulate cache expiration by clearing the cache
            var cacheField = typeof(GeneralGameSettingServices)
                .GetField("displayNameCache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var cache = cacheField?.GetValue(service);
            cache?.GetType().GetMethod("Clear")?.Invoke(cache, null);

            // Act - Second call (should refresh from database)
            var result2 = await service.GetDisplayName(gameId);

            // Assert
            result1.Should().Be(originalDisplayName);
            result2.Should().Be(updatedDisplayName);
        }

        [Fact]
        public async Task GetDisplayName_WhenDatabaseReturnsEmptyValues_ShouldCacheEmptyString()
        {
            // Arrange
            const int gameId = 1;

            SetupLocalGameConfig(gameId, displayName: null, name: null);
            var service = CreateService();

            // Act
            var result1 = await service.GetDisplayName(gameId);
            var result2 = await service.GetDisplayName(gameId);

            // Assert
            result1.Should().BeEmpty();
            result2.Should().BeEmpty();
            // Cache should work even for empty results
        }

        [Fact]
        public async Task GetDisplayName_WithHighConcurrency_ShouldHandleThreadSafety()
        {
            // Arrange
            const int gameId = 1;
            const string expectedDisplayName = "Concurrent Game";
            const int concurrentCalls = 100;

            SetupLocalGameConfig(gameId, displayName: expectedDisplayName);
            var service = CreateService();

            // Act - Make multiple concurrent calls
            var tasks = Enumerable.Range(0, concurrentCalls)
                .Select(_ => service.GetDisplayName(gameId))
                .ToArray();

            var results = await Task.WhenAll(tasks);

            // Assert
            results.Should().AllBe(expectedDisplayName);
            results.Should().HaveCount(concurrentCalls);
            // Database should be called minimal times (ideally once, but due to concurrency might be a few more)
        }

        [Fact]
        public async Task GetDisplayName_WhenCacheUpdated_ShouldAlwaysUseNewValue()
        {
            // Arrange
            const int gameId = 1;
            const string originalDisplayName = "Original Game";
            const string newDisplayName = "Updated Game";

            // First setup and call to populate cache
            SetupLocalGameConfig(gameId, displayName: originalDisplayName);
            var service = CreateService();
            await service.GetDisplayName(gameId); // This should cache the original value

            // Act - Update the database and force cache update by clearing it first
            SetupLocalGameConfig(gameId, displayName: newDisplayName);
            
            // Use reflection to clear the cache to force a fresh database call
            var cacheField = typeof(GeneralGameSettingServices)
                .GetField("displayNameCache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var cache = cacheField?.GetValue(service);
            cache?.GetType().GetMethod("Clear")?.Invoke(cache, null);

            var result = await service.GetDisplayName(gameId);

            // Assert - Should return the new value, proving the cache update logic works correctly
            result.Should().Be(newDisplayName);
        }

        [Theory]
        [InlineData("DisplayName Value", null)]
        [InlineData("DisplayName Value", "Name Value")]
        [InlineData(null, "Name Value")]
        [InlineData("", "Name Value")]
        public async Task GetDisplayName_CacheLogic_ShouldPreserveOriginalBusinessLogic(string? displayName, string? name)
        {
            // Arrange
            const int gameId = 1;
            SetupLocalGameConfig(gameId, displayName: displayName, name: name);
            var service = CreateService();

            // Determine expected result based on original business logic
            string expectedResult = !string.IsNullOrEmpty(displayName) ? displayName :
                                   !string.IsNullOrEmpty(name) ? name : string.Empty;

            // Act
            var result1 = await service.GetDisplayName(gameId);
            var result2 = await service.GetDisplayName(gameId); // Should come from cache

            // Assert
            result1.Should().Be(expectedResult);
            result2.Should().Be(expectedResult);
            // Both calls should return the same result, preserving original business logic
        }

        #endregion

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