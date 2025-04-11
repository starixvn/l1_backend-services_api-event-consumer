using ApiEventConsumer.Database.Entities.ExchangeRates;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ApiEventConsumer.Database.Tests.ExchangeRates
{
    public class ExchangeRateEntityTests
    {
        [Fact]
        public void ExchangeRateEntity_ShouldHaveCorrectCollectionName()
        {
            // Act
            var collectionName = Document.GetCollectionName(typeof(ExchangeRateEntity));

            // Assert
            Assert.Equal("exchange_rates", collectionName);
        }

        [Fact]
        public void ExchangeRateEntity_Properties_ShouldSetAndGetCorrectly()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var codes = new Dictionary<string, decimal>
            {
                { "USD", 1.0m },
                { "EUR", 0.85m },
                { "JPY", 110.0m }
            };

            var exchangeRate = new ExchangeRateEntity
            {
                Id = ObjectId.GenerateNewId(),
                Time = now,
                ExDate = now.Date,
                ExId = 123,
                Source = "API",
                Code = codes,
                CreatedAt = now,
                UpdatedAt = now.AddHours(1)
            };

            // Assert
            Assert.NotEqual(ObjectId.Empty, exchangeRate.Id);
            Assert.Equal(now, exchangeRate.Time);
            Assert.Equal(now.Date, exchangeRate.ExDate);
            Assert.Equal(123, exchangeRate.ExId);
            Assert.Equal("API", exchangeRate.Source);
            Assert.Equal(codes, exchangeRate.Code);
            Assert.Equal(3, exchangeRate.Code.Count);
            Assert.Equal(now, exchangeRate.CreatedAt);
            Assert.Equal(now.AddHours(1), exchangeRate.UpdatedAt);
        }

        [Fact]
        public void ExchangeRateEntity_DefaultValues_ShouldBeCorrect()
        {
            // Arrange
            var exchangeRate = new ExchangeRateEntity();

            // Assert
            Assert.Equal(ObjectId.Empty, exchangeRate.Id);
            Assert.Equal(default(DateTime), exchangeRate.Time);
            Assert.Equal(default(DateTime), exchangeRate.ExDate);
            Assert.Equal(0, exchangeRate.ExId);
            Assert.Null(exchangeRate.Source);
            Assert.Null(exchangeRate.Code);
            Assert.Equal(default(DateTime), exchangeRate.CreatedAt);
            Assert.Null(exchangeRate.UpdatedAt);
        }

        [Fact]
        public void ExchangeRateEntity_CodeDictionary_ShouldHandleValidValues()
        {
            // Arrange
            var exchangeRate = new ExchangeRateEntity
            {
                Code = new Dictionary<string, decimal>
                {
                    { "USD", 1.0m },
                    { "EUR", 0.85m }
                }
            };

            // Assert
            Assert.NotNull(exchangeRate.Code);
            Assert.Equal(2, exchangeRate.Code.Count);
            Assert.Equal(1.0m, exchangeRate.Code["USD"]);
            Assert.Equal(0.85m, exchangeRate.Code["EUR"]);
        }

        [Theory]
        [InlineData("API")]
        [InlineData("Manual")]
        [InlineData(null)]
        public void ExchangeRateEntity_ShouldHandleVariousSources(string? source)
        {
            // Arrange
            var exchangeRate = new ExchangeRateEntity
            {
                Source = source
            };

            // Assert
            Assert.Equal(source, exchangeRate.Source);
        }

        [Fact]
        public void ExchangeRateEntity_BsonElementAttributes_ShouldBeCorrect()
        {
            // Arrange & Act
            var type = typeof(ExchangeRateEntity);
            var timeProperty = type.GetProperty("Time");
            var exDateProperty = type.GetProperty("ExDate");
            var exIdProperty = type.GetProperty("ExId");
            var sourceProperty = type.GetProperty("Source");
            var codeProperty = type.GetProperty("Code");

            // Assert
            Assert.Equal("time", GetBsonElementValue(timeProperty));
            Assert.Equal("exDate", GetBsonElementValue(exDateProperty));
            Assert.Equal("exId", GetBsonElementValue(exIdProperty));
            Assert.Equal("source", GetBsonElementValue(sourceProperty));
            Assert.Equal("code", GetBsonElementValue(codeProperty));
        }

        [Fact]
        public void ExchangeRateEntity_CodeDictionary_ShouldHandleEmptyDictionary()
        {
            // Arrange
            var exchangeRate = new ExchangeRateEntity
            {
                Code = new Dictionary<string, decimal>()
            };

            // Assert
            Assert.NotNull(exchangeRate.Code);
            Assert.Empty(exchangeRate.Code);
        }

        [Fact]
        public void ExchangeRateEntity_CodeDictionary_ShouldHandleDecimalPrecision()
        {
            // Arrange
            var exchangeRate = new ExchangeRateEntity
            {
                Code = new Dictionary<string, decimal>
                {
                    { "USD", 1.12345m },
                    { "EUR", 0.98765m }
                }
            };

            // Assert
            Assert.Equal(1.12345m, exchangeRate.Code["USD"]);
            Assert.Equal(0.98765m, exchangeRate.Code["EUR"]);
        }

        private string GetBsonElementValue(System.Reflection.PropertyInfo? property)
        {
            var attribute = property?.GetCustomAttributes(typeof(BsonElementAttribute), true)
                .FirstOrDefault() as BsonElementAttribute;
            return attribute?.ElementName ?? string.Empty;
        }
    }
}
