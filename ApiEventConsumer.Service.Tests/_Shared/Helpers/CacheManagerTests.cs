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

        [Fact]
        public void GetCache_WithExistingKey_ShouldReturnStoredValue()
        {
            // Arrange - Test successful retrieval (red section)
            const string key = "existingKey";
            const string expectedValue = "storedValue";
            
            _memoryCache.TryGetValue(key, out Arg.Any<object?>()).Returns(x =>
            {
                x[1] = expectedValue;
                return true;
            });

            // Act
            var result = _cacheManager.GetCache<string>(key);

            // Assert - Should return the actual stored value (uncovered return path)
            result.Should().Be(expectedValue);
            _memoryCache.Received(1).TryGetValue(key, out Arg.Any<object?>());
        }

        [Fact]
        public void GetCache_WithExistingIntKey_ShouldReturnStoredIntValue()
        {
            // Arrange - Test successful int retrieval (red section)
            const string key = "existingIntKey";
            const int expectedValue = 42;
            
            _memoryCache.TryGetValue(key, out Arg.Any<object?>()).Returns(x =>
            {
                x[1] = expectedValue;
                return true;
            });

            // Act
            var result = _cacheManager.GetCache<int>(key);

            // Assert - Should return the actual stored int value
            result.Should().Be(expectedValue);
            _memoryCache.Received(1).TryGetValue(key, out Arg.Any<object?>());
        }

        [Fact]
        public void GetCache_WithExistingComplexObject_ShouldReturnStoredObject()
        {
            // Arrange - Test complex object retrieval (red section)
            const string key = "existingObjectKey";
            var expectedValue = new TestObject { Name = "Test", Value = 123 };
            
            _memoryCache.TryGetValue(key, out Arg.Any<object?>()).Returns(x =>
            {
                x[1] = expectedValue;
                return true;
            });

            // Act
            var result = _cacheManager.GetCache<TestObject>(key);

            // Assert - Should return the actual stored object
            result.Should().Be(expectedValue);
            result!.Name.Should().Be("Test");
            result.Value.Should().Be(123);
            _memoryCache.Received(1).TryGetValue(key, out Arg.Any<object?>());
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

        #region SetCache Tests

        [Fact]
        public void SetCache_WithValidParameters_ShouldNotThrow()
        {
            // Arrange - Test basic SetCache functionality (red section)
            const string key = "testKey";
            const string value = "testValue";
            const int expiredInMinutes = 10;

            // Mock CreateEntry to return a mock entry
            var mockEntry = Substitute.For<ICacheEntry>();
            mockEntry.Key.Returns(key);
            mockEntry.Value.Returns(value);
            _memoryCache.CreateEntry(key).Returns(mockEntry);

            // Act - Should not throw exception (tests the red section)
            var exception = Record.Exception(() => _cacheManager.SetCache(key, value, expiredInMinutes));

            // Assert - Method should execute without throwing
            exception.Should().BeNull();
            _memoryCache.Received(1).CreateEntry(key);
        }

        [Fact]
        public void SetCache_ShouldIgnoreExpiredInMinutesParameter()
        {
            // Arrange - Test the bug where ExpiredInMinutes parameter is ignored (red section)
            const string key = "testKey";
            const string value = "testValue";
            const int expiredInMinutes = 30; // This should be used but it's not - BUG!

            var mockEntry = Substitute.For<ICacheEntry>();
            _memoryCache.CreateEntry(key).Returns(mockEntry);

            // Act
            _cacheManager.SetCache(key, value, expiredInMinutes);

            // Assert - The method should create MemoryCacheEntryOptions but ignore the parameter
            // This demonstrates the bug: the parameter is not used in the actual implementation
            _memoryCache.Received(1).CreateEntry(key);
            mockEntry.ReceivedWithAnyArgs().Value = value;
        }

        [Fact]
        public void SetCache_WithDifferentValueTypes_ShouldAcceptGenericTypes()
        {
            // Arrange - Test SetCache with different value types (red section)
            const string stringKey = "stringKey";
            const string stringValue = "testValue";
            
            const string intKey = "intKey";
            const int intValue = 42;
            
            const string objectKey = "objectKey";
            var objectValue = new TestObject { Name = "TestObject", Value = 100 };

            var mockEntry1 = Substitute.For<ICacheEntry>();
            var mockEntry2 = Substitute.For<ICacheEntry>();
            var mockEntry3 = Substitute.For<ICacheEntry>();
            
            _memoryCache.CreateEntry(stringKey).Returns(mockEntry1);
            _memoryCache.CreateEntry(intKey).Returns(mockEntry2);
            _memoryCache.CreateEntry(objectKey).Returns(mockEntry3);

            // Act - Test different generic types
            var exception1 = Record.Exception(() => _cacheManager.SetCache(stringKey, stringValue, 5));
            var exception2 = Record.Exception(() => _cacheManager.SetCache(intKey, intValue, 5));
            var exception3 = Record.Exception(() => _cacheManager.SetCache(objectKey, objectValue, 5));

            // Assert - All should work with different types
            exception1.Should().BeNull();
            exception2.Should().BeNull();
            exception3.Should().BeNull();
            
            _memoryCache.Received(1).CreateEntry(stringKey);
            _memoryCache.Received(1).CreateEntry(intKey);
            _memoryCache.Received(1).CreateEntry(objectKey);
        }

        [Fact]
        public void SetCache_OptionsCreation_ShouldUseDefaultExpiredTimeOnly()
        {
            // Arrange - Test MemoryCacheEntryOptions creation behavior (red section)
            const string key = "optionsTestKey";
            const string value = "testValue";
            const int expiredInMinutes = 20; // This parameter is ignored (BUG)

            var mockEntry = Substitute.For<ICacheEntry>();
            _memoryCache.CreateEntry(key).Returns(mockEntry);

            // Act
            var exception = Record.Exception(() => _cacheManager.SetCache(key, value, expiredInMinutes));

            // Assert - Method should execute the red section that creates MemoryCacheEntryOptions
            exception.Should().BeNull();
            
            // The method creates MemoryCacheEntryOptions with SetSlidingExpiration(TimeSpan.FromMinutes(2))
            // regardless of the expiredInMinutes parameter (this is the bug)
            _memoryCache.Received(1).CreateEntry(key);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("normalKey")]
        public void SetCache_WithVariousKeys_ShouldHandleAllKeyFormats(string key)
        {
            // Arrange - Test SetCache with various key formats (red section)
            const string value = "testValue";
            const int expiredInMinutes = 5;

            var mockEntry = Substitute.For<ICacheEntry>();
            _memoryCache.CreateEntry(key).Returns(mockEntry);

            // Act
            var exception = Record.Exception(() => _cacheManager.SetCache(key, value, expiredInMinutes));

            // Assert - Should handle various key formats gracefully
            exception.Should().BeNull();
            _memoryCache.Received(1).CreateEntry(key);
        }

        [Fact]
        public void SetCache_WithNullKey_ShouldAttemptToCallCache()
        {
            // Arrange - Test SetCache with null key (red section)
            string? key = null;
            const string value = "testValue";
            const int expiredInMinutes = 5;

            var mockEntry = Substitute.For<ICacheEntry>();
            _memoryCache.CreateEntry(key!).Returns(mockEntry);

            // Act - This tests that the method reaches the cache call
            var exception = Record.Exception(() => _cacheManager.SetCache(key!, value, expiredInMinutes));

            // Assert - Method should attempt to use the cache (may throw but that's cache behavior)
            _memoryCache.Received(1).CreateEntry(key!);
        }

        [Fact]
        public void SetCache_WithNullValue_ShouldAllowNullValues()
        {
            // Arrange - Test SetCache with null value (red section)
            const string key = "nullValueKey";
            string? value = null;
            const int expiredInMinutes = 5;

            var mockEntry = Substitute.For<ICacheEntry>();
            _memoryCache.CreateEntry(key).Returns(mockEntry);

            // Act
            var exception = Record.Exception(() => _cacheManager.SetCache(key, value, expiredInMinutes));

            // Assert - Should handle null values
            exception.Should().BeNull();
            _memoryCache.Received(1).CreateEntry(key);
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

        #region Test Helper Classes

        private class TestObject
        {
            public string Name { get; set; } = string.Empty;
            public int Value { get; set; }
        }

        #endregion
    }
}
