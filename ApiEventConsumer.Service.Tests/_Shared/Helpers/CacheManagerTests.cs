using ApiEventConsumer.Api._Shared.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;

namespace ApiEventConsumer.Service.Tests._Shared.Helpers
{
    public class CacheManagerTests
    {
        private readonly IMemoryCache _memoryCache;
        private readonly CacheManager _cacheManager;

        public CacheManagerTests()
        {
            _memoryCache = Substitute.For<IMemoryCache>();
            _cacheManager = new CacheManager(_memoryCache);
        }

        [Fact]
        public void Constructor_WithValidMemoryCache_ShouldCreateInstance()
        {
            // Arrange & Act
            var manager = new CacheManager(_memoryCache);

            // Assert
            manager.Should().NotBeNull();
            manager.Should().BeAssignableTo<ICacheManager>();
        }

        #region GetCache Tests

        [Fact]
        public void GetCache_WithNonExistentKey_ShouldReturnDefault()
        {
            // Arrange
            const string key = "nonExistentKey";
            
            object? outValue = null;
            _memoryCache.TryGetValue(key, out outValue).Returns(false);

            // Act
            var result = _cacheManager.GetCache<string>(key);

            // Assert
            result.Should().BeNull();
            _memoryCache.Received(1).TryGetValue(key, out Arg.Any<object?>());
        }

        [Fact]
        public void GetCache_WithNonExistentIntKey_ShouldReturnDefault()
        {
            // Arrange
            const string key = "nonExistentIntKey";
            
            object? outValue = null;
            _memoryCache.TryGetValue(key, out outValue).Returns(false);

            // Act
            var result = _cacheManager.GetCache<int>(key);

            // Assert
            result.Should().Be(default(int));
            _memoryCache.Received(1).TryGetValue(key, out Arg.Any<object?>());
        }

        [Fact]
        public void GetCache_WithEmptyKey_ShouldCallTryGetValue()
        {
            // Arrange
            const string key = "";
            
            object? outValue = null;
            _memoryCache.TryGetValue(key, out outValue).Returns(false);

            // Act
            var result = _cacheManager.GetCache<string>(key);

            // Assert
            result.Should().BeNull();
            _memoryCache.Received(1).TryGetValue(key, out Arg.Any<object?>());
        }

        [Fact]
        public void GetCache_WithNullKey_ShouldCallTryGetValue()
        {
            // Arrange
            string? key = null;
            
            object? outValue = null;
            _memoryCache.TryGetValue(key!, out outValue).Returns(false);

            // Act
            var result = _cacheManager.GetCache<string>(key!);

            // Assert
            result.Should().BeNull();
            _memoryCache.Received(1).TryGetValue(key!, out Arg.Any<object?>());
        }

        #endregion

        #region CachedKeys Enum Tests

        [Fact]
        public void CachedKeys_Enum_ShouldHaveExpectedValues()
        {
            // Act & Assert
            var enumValues = Enum.GetValues<CachedKeys>();
            
            enumValues.Should().Contain(CachedKeys.ExchangeRate);
            enumValues.Should().Contain(CachedKeys.Currencies);
            enumValues.Should().Contain(CachedKeys.User);
            enumValues.Should().Contain(CachedKeys.RunningPromotions);
            
            enumValues.Should().HaveCount(4);
        }

        [Fact]
        public void CachedKeys_Enum_ShouldHaveCorrectStringRepresentation()
        {
            // Act & Assert
            CachedKeys.ExchangeRate.ToString().Should().Be("ExchangeRate");
            CachedKeys.Currencies.ToString().Should().Be("Currencies");
            CachedKeys.User.ToString().Should().Be("User");
            CachedKeys.RunningPromotions.ToString().Should().Be("RunningPromotions");
        }

        #endregion

        #region Type Safety Tests

        [Fact]
        public void GetCache_WithWrongType_ShouldReturnDefault()
        {
            // Arrange
            const string key = "typeTestKey";
            const string storedValue = "stringValue";
            
            object? outValue = storedValue;
            _memoryCache.TryGetValue(key, out outValue).Returns(x =>
            {
                x[1] = storedValue;
                return true;
            });

            // Act - Try to get as int when string was stored
            var result = _cacheManager.GetCache<int>(key);

            // Assert
            // This will likely throw InvalidCastException or return default depending on implementation
            result.Should().Be(default(int));
        }
        #endregion
    }
}
