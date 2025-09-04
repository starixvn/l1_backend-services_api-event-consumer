using ApiEventConsumer.Api._Shared.Helpers;
using Microsoft.Extensions.Caching.Memory;

namespace ApiEventConsumer.Service.Tests._Shared.Helpers
{
    public class SharedCacheServiceTests
    {
        private readonly SharedCacheService _cacheService;
        private readonly string _testKeyPrefix;

        public SharedCacheServiceTests()
        {
            _cacheService = new SharedCacheService();
            // Use unique prefix to avoid test interference since cache is static
            _testKeyPrefix = $"test_{Guid.NewGuid():N}_";
        }

        private string GetUniqueKey(string key) => $"{_testKeyPrefix}{key}";

        [Fact]
        public void Set_WithStringValue_ShouldStoreValue()
        {
            // Arrange
            var key = GetUniqueKey("string_test");
            var value = "test value";
            var expiration = TimeSpan.FromMinutes(5);

            // Act
            _cacheService.Set(key, value, expiration);

            // Assert
            var result = _cacheService.Get<string>(key);
            Assert.Equal(value, result);
        }

        [Fact]
        public void Set_WithIntValue_ShouldStoreValue()
        {
            // Arrange
            var key = GetUniqueKey("int_test");
            var value = 42;
            var expiration = TimeSpan.FromMinutes(5);

            // Act
            _cacheService.Set(key, value, expiration);

            // Assert
            var result = _cacheService.Get<int>(key);
            Assert.Equal(value, result);
        }

        [Fact]
        public void Set_WithComplexObject_ShouldStoreValue()
        {
            // Arrange
            var key = GetUniqueKey("object_test");
            var value = new TestObject { Name = "Test", Value = 123 };
            var expiration = TimeSpan.FromMinutes(5);

            // Act
            _cacheService.Set(key, value, expiration);

            // Assert
            var result = _cacheService.Get<TestObject>(key);
            Assert.NotNull(result);
            Assert.Equal(value.Name, result.Name);
            Assert.Equal(value.Value, result.Value);
        }

        [Fact]
        public void Set_WithNullValue_ShouldStoreNull()
        {
            // Arrange
            var key = GetUniqueKey("null_test");
            string? nullValue = null;
            var expiration = TimeSpan.FromMinutes(5);

            // Act
            _cacheService.Set(key, nullValue, expiration);

            // Assert
            var result = _cacheService.Get<string>(key);
            Assert.Null(result);
        }

        [Fact]
        public void Set_WithEmptyString_ShouldStoreEmptyString()
        {
            // Arrange
            var key = GetUniqueKey("empty_string_test");
            var value = string.Empty;
            var expiration = TimeSpan.FromMinutes(5);

            // Act
            _cacheService.Set(key, value, expiration);

            // Assert
            var result = _cacheService.Get<string>(key);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void Set_WithZeroTimeSpan_ShouldThrowArgumentOutOfRangeException()
        {
            // Arrange
            var key = GetUniqueKey("zero_expiration_test");
            var value = "test";
            var expiration = TimeSpan.Zero;

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _cacheService.Set(key, value, expiration));
        }

        [Fact]
        public void Get_WithExistingKey_ShouldReturnValue()
        {
            // Arrange
            var key = GetUniqueKey("get_existing_test");
            var value = "cached value";
            _cacheService.Set(key, value, TimeSpan.FromMinutes(5));

            // Act
            var result = _cacheService.Get<string>(key);

            // Assert
            Assert.Equal(value, result);
        }

        [Fact]
        public void Get_WithNonExistingKey_ShouldReturnDefault()
        {
            // Arrange
            var key = GetUniqueKey("non_existing_key");

            // Act
            var stringResult = _cacheService.Get<string>(key);
            var intResult = _cacheService.Get<int>(key);
            var objectResult = _cacheService.Get<TestObject>(key);

            // Assert
            Assert.Null(stringResult);
            Assert.Equal(0, intResult); // default(int) = 0
            Assert.Null(objectResult);
        }

        [Fact]
        public void Get_WithWrongType_ShouldThrowInvalidCastException()
        {
            // Arrange
            var key = GetUniqueKey("wrong_type_test");
            _cacheService.Set(key, "string value", TimeSpan.FromMinutes(5));

            // Act & Assert
            Assert.Throws<InvalidCastException>(() => _cacheService.Get<int>(key));
        }

        [Fact]
        public void Get_WithNullableType_ShouldHandleCorrectly()
        {
            // Arrange
            var key = GetUniqueKey("nullable_test");
            int? nullableValue = 42;
            _cacheService.Set(key, nullableValue, TimeSpan.FromMinutes(5));

            // Act
            var result = _cacheService.Get<int?>(key);

            // Assert
            Assert.Equal(nullableValue, result);
        }

        [Fact]
        public void TryGet_WithExistingKey_ShouldReturnTrueAndSetValue()
        {
            // Arrange
            var key = GetUniqueKey("tryget_existing_test");
            var value = "test value";
            _cacheService.Set(key, value, TimeSpan.FromMinutes(5));

            // Act
            var result = _cacheService.TryGet<string>(key, out var retrievedValue);

            // Assert
            Assert.True(result);
            Assert.Equal(value, retrievedValue);
        }

        [Fact]
        public void TryGet_WithNonExistingKey_ShouldReturnFalseAndSetDefault()
        {
            // Arrange
            var key = GetUniqueKey("tryget_non_existing_test");

            // Act
            var result = _cacheService.TryGet<string>(key, out var retrievedValue);

            // Assert
            Assert.False(result);
            Assert.Null(retrievedValue);
        }

        [Fact]
        public void TryGet_WithIntValue_ShouldReturnCorrectValue()
        {
            // Arrange
            var key = GetUniqueKey("tryget_int_test");
            var value = 999;
            _cacheService.Set(key, value, TimeSpan.FromMinutes(5));

            // Act
            var result = _cacheService.TryGet<int>(key, out var retrievedValue);

            // Assert
            Assert.True(result);
            Assert.Equal(value, retrievedValue);
        }

        [Fact]
        public void TryGet_WithComplexObject_ShouldReturnCorrectValue()
        {
            // Arrange
            var key = GetUniqueKey("tryget_object_test");
            var value = new TestObject { Name = "Complex Object", Value = 456 };
            _cacheService.Set(key, value, TimeSpan.FromMinutes(5));

            // Act
            var result = _cacheService.TryGet<TestObject>(key, out var retrievedValue);

            // Assert
            Assert.True(result);
            Assert.NotNull(retrievedValue);
            Assert.Equal(value.Name, retrievedValue.Name);
            Assert.Equal(value.Value, retrievedValue.Value);
        }

        [Fact]
        public void TryGet_WithWrongType_ShouldThrowInvalidCastException()
        {
            // Arrange
            var key = GetUniqueKey("tryget_wrong_type_test");
            _cacheService.Set(key, "string value", TimeSpan.FromMinutes(5));

            // Act & Assert
            Assert.Throws<InvalidCastException>(() => _cacheService.TryGet<int>(key, out var retrievedValue));
        }

        [Fact]
        public void TryGet_WithNullValue_ShouldReturnTrueWithNull()
        {
            // Arrange
            var key = GetUniqueKey("tryget_null_test");
            string? nullValue = null;
            _cacheService.Set(key, nullValue, TimeSpan.FromMinutes(5));

            // Act
            var result = _cacheService.TryGet<string>(key, out var retrievedValue);

            // Assert
            Assert.True(result); // Should return true even for null values
            Assert.Null(retrievedValue);
        }

        [Fact]
        public void SetAndGet_MultipleOperations_ShouldWorkCorrectly()
        {
            // Arrange
            var key1 = GetUniqueKey("multi_test_1");
            var key2 = GetUniqueKey("multi_test_2");
            var value1 = "first value";
            var value2 = 42;

            // Act
            _cacheService.Set(key1, value1, TimeSpan.FromMinutes(5));
            _cacheService.Set(key2, value2, TimeSpan.FromMinutes(5));

            // Assert
            Assert.Equal(value1, _cacheService.Get<string>(key1));
            Assert.Equal(value2, _cacheService.Get<int>(key2));

            Assert.True(_cacheService.TryGet<string>(key1, out var retrieved1));
            Assert.True(_cacheService.TryGet<int>(key2, out var retrieved2));
            Assert.Equal(value1, retrieved1);
            Assert.Equal(value2, retrieved2);
        }

        [Fact]
        public void Set_OverwriteExistingKey_ShouldUpdateValue()
        {
            // Arrange
            var key = GetUniqueKey("overwrite_test");
            var oldValue = "old value";
            var newValue = "new value";

            // Act
            _cacheService.Set(key, oldValue, TimeSpan.FromMinutes(5));
            _cacheService.Set(key, newValue, TimeSpan.FromMinutes(5)); // Overwrite

            // Assert
            var result = _cacheService.Get<string>(key);
            Assert.Equal(newValue, result);
        }

        [Fact]
        public void Set_WithDifferentExpirations_ShouldStoreCorrectly()
        {
            // Arrange
            var key1 = GetUniqueKey("exp_test_1");
            var key2 = GetUniqueKey("exp_test_2");
            var value1 = "short expiration";
            var value2 = "long expiration";

            // Act
            _cacheService.Set(key1, value1, TimeSpan.FromMilliseconds(100)); // Short but positive
            _cacheService.Set(key2, value2, TimeSpan.FromHours(1)); // Long

            // Assert - Both should be retrievable immediately
            Assert.Equal(value1, _cacheService.Get<string>(key1));
            Assert.Equal(value2, _cacheService.Get<string>(key2));
        }

        [Fact]
        public void Constructor_ShouldCreateInstance()
        {
            // Act
            var service = new SharedCacheService();

            // Assert
            Assert.NotNull(service);
            Assert.IsAssignableFrom<ISharedCacheService>(service);
        }

        [Fact]
        public void Interface_ShouldHaveExpectedMethods()
        {
            // Arrange
            var interfaceType = typeof(ISharedCacheService);

            // Act
            var methods = interfaceType.GetMethods();

            // Assert
            Assert.Contains(methods, m => m.Name == "Set" && m.IsGenericMethod);
            Assert.Contains(methods, m => m.Name == "Get" && m.IsGenericMethod);
            Assert.Contains(methods, m => m.Name == "TryGet" && m.IsGenericMethod);

            var setMethod = methods.First(m => m.Name == "Set");
            var getMethod = methods.First(m => m.Name == "Get");
            var tryGetMethod = methods.First(m => m.Name == "TryGet");

            // Verify Set method signature
            var setParams = setMethod.GetParameters();
            Assert.Equal(3, setParams.Length);
            Assert.Equal(typeof(string), setParams[0].ParameterType);
            Assert.Equal(typeof(TimeSpan), setParams[2].ParameterType);

            // Verify Get method signature
            var getParams = getMethod.GetParameters();
            Assert.Equal(1, getParams.Length);
            Assert.Equal(typeof(string), getParams[0].ParameterType);

            // Verify TryGet method signature
            var tryGetParams = tryGetMethod.GetParameters();
            Assert.Equal(2, tryGetParams.Length);
            Assert.Equal(typeof(string), tryGetParams[0].ParameterType);
            Assert.True(tryGetParams[1].IsOut);
        }

        [Fact]
        public void Get_AfterSetWithSameType_ShouldPreserveType()
        {
            // Arrange
            var key = GetUniqueKey("type_preservation_test");
            var dateTimeValue = DateTime.UtcNow;
            var boolValue = true;
            var doubleValue = 3.14159;

            // Act & Assert
            _cacheService.Set(key + "_datetime", dateTimeValue, TimeSpan.FromMinutes(5));
            _cacheService.Set(key + "_bool", boolValue, TimeSpan.FromMinutes(5));
            _cacheService.Set(key + "_double", doubleValue, TimeSpan.FromMinutes(5));

            Assert.Equal(dateTimeValue, _cacheService.Get<DateTime>(key + "_datetime"));
            Assert.Equal(boolValue, _cacheService.Get<bool>(key + "_bool"));
            Assert.Equal(doubleValue, _cacheService.Get<double>(key + "_double"));
        }

        [Fact]
        public void TryGet_WithDifferentDataTypes_ShouldHandleCorrectly()
        {
            // Arrange
            var baseKey = GetUniqueKey("tryget_types");

            // Act & Assert for various types
            _cacheService.Set(baseKey + "_list", new List<string> { "item1", "item2" }, TimeSpan.FromMinutes(5));
            _cacheService.Set(baseKey + "_dict", new Dictionary<string, int> { { "key1", 1 } }, TimeSpan.FromMinutes(5));

            Assert.True(_cacheService.TryGet<List<string>>(baseKey + "_list", out var listResult));
            Assert.NotNull(listResult);
            Assert.Equal(2, listResult.Count);

            Assert.True(_cacheService.TryGet<Dictionary<string, int>>(baseKey + "_dict", out var dictResult));
            Assert.NotNull(dictResult);
            Assert.Single(dictResult);
            Assert.Equal(1, dictResult["key1"]);
        }

        [Fact]
        public void Set_WithEmptyKey_ShouldNotThrow()
        {
            // Arrange
            var key = string.Empty;
            var value = "test";

            // Act & Assert
            var exception = Record.Exception(() => _cacheService.Set(key, value, TimeSpan.FromMinutes(5)));
            Assert.Null(exception);
        }

        [Fact]
        public void Set_WithNegativeTimeSpan_ShouldThrowArgumentOutOfRangeException()
        {
            // Arrange
            var key = GetUniqueKey("negative_expiration_test");
            var value = "test";
            var expiration = TimeSpan.FromMinutes(-1);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _cacheService.Set(key, value, expiration));
        }

        [Fact]
        public void Get_WithEmptyKey_ShouldReturnDefault()
        {
            // Arrange
            var key = string.Empty;

            // Act
            var result = _cacheService.Get<string>(key);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void TryGet_WithEmptyKey_ShouldReturnFalse()
        {
            // Arrange
            var key = string.Empty;

            // Act
            var result = _cacheService.TryGet<string>(key, out var value);

            // Assert
            Assert.False(result);
            Assert.Null(value);
        }

        [Fact]
        public void StaticCache_ShouldPersistAcrossInstances()
        {
            // Arrange
            var key = GetUniqueKey("static_test");
            var value = "persistent value";
            var service1 = new SharedCacheService();
            var service2 = new SharedCacheService();

            // Act
            service1.Set(key, value, TimeSpan.FromMinutes(5));

            // Assert
            var result = service2.Get<string>(key);
            Assert.Equal(value, result);
        }

        [Fact]
        public void Set_WithLongExpiration_ShouldStoreCorrectly()
        {
            // Arrange
            var key = GetUniqueKey("long_expiration_test");
            var value = "long lasting value";
            var expiration = TimeSpan.FromDays(1);

            // Act
            _cacheService.Set(key, value, expiration);

            // Assert
            var result = _cacheService.Get<string>(key);
            Assert.Equal(value, result);

            Assert.True(_cacheService.TryGet<string>(key, out var tryGetResult));
            Assert.Equal(value, tryGetResult);
        }

        [Fact]
        public void Get_TypeCasting_ShouldThrowForIncompatibleTypes()
        {
            // Arrange
            var key = GetUniqueKey("type_casting_test");
            var stringValue = "not a number";
            _cacheService.Set(key, stringValue, TimeSpan.FromMinutes(5));

            // Act & Assert
            Assert.Throws<InvalidCastException>(() => _cacheService.Get<int>(key));
            Assert.Throws<InvalidCastException>(() => _cacheService.Get<double>(key));
        }

        [Fact]
        public void TryGet_TypeCasting_ShouldThrowForIncompatibleTypes()
        {
            // Arrange
            var key = GetUniqueKey("tryget_type_casting_test");
            var stringValue = "not a number";
            _cacheService.Set(key, stringValue, TimeSpan.FromMinutes(5));

            // Act & Assert
            Assert.Throws<InvalidCastException>(() => _cacheService.TryGet<int>(key, out var intValue));
            Assert.Throws<InvalidCastException>(() => _cacheService.TryGet<double>(key, out var doubleValue));
        }

        [Fact]
        public void Interface_Implementation_ShouldMatchContract()
        {
            // Arrange & Act
            var service = new SharedCacheService();

            // Assert
            Assert.IsAssignableFrom<ISharedCacheService>(service);
            
            // Verify interface has expected methods
            var interfaceType = typeof(ISharedCacheService);
            var methods = interfaceType.GetMethods();
            
            Assert.Contains(methods, m => m.Name == "Set");
            Assert.Contains(methods, m => m.Name == "Get");
            Assert.Contains(methods, m => m.Name == "TryGet");
            
            // All methods should be generic
            Assert.True(methods.Any(m => m.Name == "Set" && m.IsGenericMethod));
            Assert.True(methods.Any(m => m.Name == "Get" && m.IsGenericMethod));
            Assert.True(methods.Any(m => m.Name == "TryGet" && m.IsGenericMethod));
        }
    }

    // Test helper class
    public class TestObject
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}
