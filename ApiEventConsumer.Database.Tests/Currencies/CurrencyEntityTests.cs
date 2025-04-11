using ApiEventConsumer.Database.Entities.Currencies;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ApiEventConsumer.Database.Tests.Currencies
{
    public class CurrencyEntityTests
    {
        [Fact]
        public void CurrencyEntity_ShouldHaveCorrectCollectionName()
        {
            // Act
            var collectionName = Document.GetCollectionName(typeof(CurrencyEntity));

            // Assert
            Assert.Equal("currencies", collectionName);
        }

        [Fact]
        public void CurrencyEntity_Properties_ShouldSetAndGetCorrectly()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var currency = new CurrencyEntity
            {
                Id = ObjectId.GenerateNewId(),
                Name = "US Dollar",
                Code = "USD",
                Multiplier = 1.0,
                Enabled = true,
                IsTest = false,
                PartnerId = "partner123",
                GroupId = ObjectId.GenerateNewId(),
                IsUncountOnTotal = false,
                CreatedAt = now,
                UpdatedAt = now.AddHours(1)
            };

            // Assert
            Assert.NotEqual(ObjectId.Empty, currency.Id);
            Assert.Equal("US Dollar", currency.Name);
            Assert.Equal("USD", currency.Code);
            Assert.Equal(1.0, currency.Multiplier);
            Assert.True(currency.Enabled);
            Assert.False(currency.IsTest);
            Assert.Equal("partner123", currency.PartnerId);
            Assert.NotEqual(ObjectId.Empty, currency.GroupId);
            Assert.False(currency.IsUncountOnTotal);
            Assert.Equal(now, currency.CreatedAt);
            Assert.Equal(now.AddHours(1), currency.UpdatedAt);
        }

        [Fact]
        public void CurrencyEntity_DefaultValues_ShouldBeCorrect()
        {
            // Arrange
            var currency = new CurrencyEntity();

            // Assert
            Assert.Equal(ObjectId.Empty, currency.Id);
            Assert.Null(currency.Name);
            Assert.Null(currency.Code);
            Assert.Equal(0.0, currency.Multiplier);
            Assert.False(currency.Enabled);
            Assert.False(currency.IsTest);
            Assert.Null(currency.PartnerId);
            Assert.Equal(ObjectId.Empty, currency.GroupId);
            Assert.False(currency.IsUncountOnTotal);
            Assert.Equal(default(DateTime), currency.CreatedAt);
            Assert.Null(currency.UpdatedAt);
        }

        [Theory]
        [InlineData("US Dollar", "USD", 1.0)]
        [InlineData("Euro", "EUR", 0.85)]
        [InlineData("Japanese Yen", "JPY", 110.0)]
        public void CurrencyEntity_ShouldHandleVariousCurrencies(string name, string code, double multiplier)
        {
            // Arrange
            var currency = new CurrencyEntity
            {
                Name = name,
                Code = code,
                Multiplier = multiplier
            };

            // Assert
            Assert.Equal(name, currency.Name);
            Assert.Equal(code, currency.Code);
            Assert.Equal(multiplier, currency.Multiplier);
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        public void CurrencyEntity_ShouldHandleVariousStates(bool enabled, bool isTest)
        {
            // Arrange
            var currency = new CurrencyEntity
            {
                Enabled = enabled,
                IsTest = isTest
            };

            // Assert
            Assert.Equal(enabled, currency.Enabled);
            Assert.Equal(isTest, currency.IsTest);
        }

        [Fact]
        public void CurrencyEntity_GroupId_ShouldHandleValidObjectId()
        {
            // Arrange
            var groupId = ObjectId.GenerateNewId();
            var currency = new CurrencyEntity
            {
                GroupId = groupId
            };

            // Assert
            Assert.Equal(groupId, currency.GroupId);
            Assert.NotEqual(ObjectId.Empty, currency.GroupId);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(1.0)]
        [InlineData(100.0)]
        [InlineData(-1.0)]
        public void CurrencyEntity_ShouldHandleVariousMultipliers(double multiplier)
        {
            // Arrange
            var currency = new CurrencyEntity
            {
                Multiplier = multiplier
            };

            // Assert
            Assert.Equal(multiplier, currency.Multiplier);
        }

        [Fact]
        public void CurrencyEntity_BsonElementAttributes_ShouldBeCorrect()
        {
            // Arrange & Act
            var type = typeof(CurrencyEntity);
            var nameProperty = type.GetProperty("Name");
            var codeProperty = type.GetProperty("Code");
            var multiplierProperty = type.GetProperty("Multiplier");
            var enabledProperty = type.GetProperty("Enabled");
            var isTestProperty = type.GetProperty("IsTest");
            var partnerIdProperty = type.GetProperty("PartnerId");
            var groupIdProperty = type.GetProperty("GroupId");
            var isUncountOnTotalProperty = type.GetProperty("IsUncountOnTotal");

            // Assert
            Assert.Equal("name", GetBsonElementValue(nameProperty));
            Assert.Equal("code", GetBsonElementValue(codeProperty));
            Assert.Equal("multiplier", GetBsonElementValue(multiplierProperty));
            Assert.Equal("enabled", GetBsonElementValue(enabledProperty));
            Assert.Equal("isTest", GetBsonElementValue(isTestProperty));
            Assert.Equal("partnerId", GetBsonElementValue(partnerIdProperty));
            Assert.Equal("groupId", GetBsonElementValue(groupIdProperty));
            Assert.Equal("isUncountOnTotal", GetBsonElementValue(isUncountOnTotalProperty));
        }

        private static string GetBsonElementValue(System.Reflection.PropertyInfo? property)
        {
            var attribute = property?.GetCustomAttributes(typeof(BsonElementAttribute), true)
                .FirstOrDefault() as BsonElementAttribute;
            return attribute?.ElementName ?? string.Empty;
        }
    }
}
