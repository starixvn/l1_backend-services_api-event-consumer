using ApiEventConsumer.Database.Attributes;
using ApiEventConsumer.Database.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Reflection;

namespace ApiEventConsumer.Database.Tests.Entities
{
    public class UserSessionEntityTests
    {
        [Fact]
        public void Constructor_ShouldCreateInstance()
        {
            // Act
            var entity = new UserSessionEntity();

            // Assert
            Assert.IsAssignableFrom<Document>(entity);
            Assert.IsAssignableFrom<IDocument>(entity);
        }

        [Fact]
        public void Properties_ShouldHaveCorrectDefaultValues()
        {
            // Act
            var entity = new UserSessionEntity();

            // Assert
            Assert.Equal(default(DateTime), entity.RecheckInTime);
            Assert.Null(entity.ParnerId);
            Assert.Null(entity.BrowserUserAgent);
            Assert.Null(entity.PlayerId);
            Assert.Null(entity.Ip);
        }

        [Fact]
        public void RecheckInTime_ShouldSetAndGetValue()
        {
            // Arrange
            var entity = new UserSessionEntity();
            var testDateTime = new DateTime(2024, 1, 15, 10, 30, 45);

            // Act
            entity.RecheckInTime = testDateTime;

            // Assert
            Assert.Equal(testDateTime, entity.RecheckInTime);
        }

        [Fact]
        public void ParnerId_ShouldSetAndGetValue()
        {
            // Arrange
            var entity = new UserSessionEntity();
            const string testValue = "partner-123";

            // Act
            entity.ParnerId = testValue;

            // Assert
            Assert.Equal(testValue, entity.ParnerId);
        }

        [Fact]
        public void BrowserUserAgent_ShouldSetAndGetValue()
        {
            // Arrange
            var entity = new UserSessionEntity();
            const string testValue = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";

            // Act
            entity.BrowserUserAgent = testValue;

            // Assert
            Assert.Equal(testValue, entity.BrowserUserAgent);
        }

        [Fact]
        public void PlayerId_ShouldSetAndGetValue()
        {
            // Arrange
            var entity = new UserSessionEntity();
            const string testValue = "player-456";

            // Act
            entity.PlayerId = testValue;

            // Assert
            Assert.Equal(testValue, entity.PlayerId);
        }

        [Fact]
        public void Ip_ShouldSetAndGetValue()
        {
            // Arrange
            var entity = new UserSessionEntity();
            const string testValue = "192.168.1.100";

            // Act
            entity.Ip = testValue;

            // Assert
            Assert.Equal(testValue, entity.Ip);
        }

        [Fact]
        public void Class_ShouldHaveBsonCollectionAttribute()
        {
            // Act
            var attribute = typeof(UserSessionEntity)
                .GetCustomAttribute<BsonCollectionAttribute>();

            // Assert
            Assert.NotNull(attribute);
            Assert.Equal("usersessions", attribute.CollectionName);
        }

        [Fact]
        public void RecheckInTime_ShouldHaveBsonElementAttribute()
        {
            // Act
            var property = typeof(UserSessionEntity).GetProperty(nameof(UserSessionEntity.RecheckInTime));
            var attribute = property!.GetCustomAttribute<BsonElementAttribute>();

            // Assert
            Assert.NotNull(attribute);
            Assert.Equal("recheckInTime", attribute.ElementName);
        }

        [Fact]
        public void ParnerId_ShouldHaveBsonElementAttribute()
        {
            // Act
            var property = typeof(UserSessionEntity).GetProperty(nameof(UserSessionEntity.ParnerId));
            var attribute = property!.GetCustomAttribute<BsonElementAttribute>();

            // Assert
            Assert.NotNull(attribute);
            Assert.Equal("partnerId", attribute.ElementName);
        }

        [Fact]
        public void BrowserUserAgent_ShouldHaveBsonElementAttribute()
        {
            // Act
            var property = typeof(UserSessionEntity).GetProperty(nameof(UserSessionEntity.BrowserUserAgent));
            var attribute = property!.GetCustomAttribute<BsonElementAttribute>();

            // Assert
            Assert.NotNull(attribute);
            Assert.Equal("browserUserAgent", attribute.ElementName);
        }

        [Fact]
        public void PlayerId_ShouldHaveBsonElementAttribute()
        {
            // Act
            var property = typeof(UserSessionEntity).GetProperty(nameof(UserSessionEntity.PlayerId));
            var attribute = property!.GetCustomAttribute<BsonElementAttribute>();

            // Assert
            Assert.NotNull(attribute);
            Assert.Equal("playerId", attribute.ElementName);
        }

        [Fact]
        public void Ip_ShouldHaveBsonElementAttribute()
        {
            // Act
            var property = typeof(UserSessionEntity).GetProperty(nameof(UserSessionEntity.Ip));
            var attribute = property!.GetCustomAttribute<BsonElementAttribute>();

            // Assert
            Assert.NotNull(attribute);
            Assert.Equal("ip", attribute.ElementName);
        }

        [Fact]
        public void AllStringProperties_ShouldBeNullable()
        {
            // Act & Assert
            var parnerIdProperty = typeof(UserSessionEntity).GetProperty(nameof(UserSessionEntity.ParnerId));
            var browserUserAgentProperty = typeof(UserSessionEntity).GetProperty(nameof(UserSessionEntity.BrowserUserAgent));
            var playerIdProperty = typeof(UserSessionEntity).GetProperty(nameof(UserSessionEntity.PlayerId));
            var ipProperty = typeof(UserSessionEntity).GetProperty(nameof(UserSessionEntity.Ip));

            // All string properties should be nullable
            Assert.Equal(typeof(string), parnerIdProperty!.PropertyType);
            Assert.Equal(typeof(string), browserUserAgentProperty!.PropertyType);
            Assert.Equal(typeof(string), playerIdProperty!.PropertyType);
            Assert.Equal(typeof(string), ipProperty!.PropertyType);
        }

        [Fact]
        public void RecheckInTime_ShouldBeDateTime()
        {
            // Act & Assert
            var property = typeof(UserSessionEntity).GetProperty(nameof(UserSessionEntity.RecheckInTime));
            
            Assert.Equal(typeof(DateTime), property!.PropertyType);
        }

        [Fact]
        public void Entity_ShouldInheritFromDocument()
        {
            // Act
            var entity = new UserSessionEntity();

            // Assert
            Assert.True(entity is Document);
            
            // Should have inherited properties from Document
            // ObjectId is a value type, so it cannot be null
            Assert.Equal(default(DateTime), entity.CreatedAt);
            Assert.Null(entity.UpdatedAt);
            
            // Verify inherited property types
            Assert.Equal(typeof(ObjectId), entity.Id.GetType());
            Assert.Equal(typeof(DateTime), entity.CreatedAt.GetType());
        }

        [Fact]
        public void Entity_ShouldHaveAllExpectedProperties()
        {
            // Act
            var properties = typeof(UserSessionEntity).GetProperties()
                .Where(p => p.DeclaringType == typeof(UserSessionEntity))
                .Select(p => p.Name)
                .ToList();

            // Assert
            Assert.Equal(5, properties.Count);
            Assert.Contains("RecheckInTime", properties);
            Assert.Contains("ParnerId", properties); // Note: This has a typo in the actual code
            Assert.Contains("BrowserUserAgent", properties);
            Assert.Contains("PlayerId", properties);
            Assert.Contains("Ip", properties);
        }

        [Fact]
        public void Entity_WithCompleteData_ShouldSetAllProperties()
        {
            // Arrange
            var testDateTime = new DateTime(2024, 2, 20, 14, 30, 0);
            const string testPartnerId = "partner-789";
            const string testUserAgent = "Chrome/120.0.0.0";
            const string testPlayerId = "player-999";
            const string testIp = "10.0.0.1";

            // Act
            var entity = new UserSessionEntity
            {
                RecheckInTime = testDateTime,
                ParnerId = testPartnerId,
                BrowserUserAgent = testUserAgent,
                PlayerId = testPlayerId,
                Ip = testIp
            };

            // Assert
            Assert.Equal(testDateTime, entity.RecheckInTime);
            Assert.Equal(testPartnerId, entity.ParnerId);
            Assert.Equal(testUserAgent, entity.BrowserUserAgent);
            Assert.Equal(testPlayerId, entity.PlayerId);
            Assert.Equal(testIp, entity.Ip);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("valid-partner-id")]
        [InlineData("special-chars-!@#$%")]
        public void ParnerId_ShouldAcceptVariousStringValues(string value)
        {
            // Arrange
            var entity = new UserSessionEntity();

            // Act
            entity.ParnerId = value;

            // Assert
            Assert.Equal(value, entity.ParnerId);
        }

        [Theory]
        [InlineData("127.0.0.1")]
        [InlineData("192.168.1.1")]
        [InlineData("10.0.0.100")]
        [InlineData("2001:db8::1")]
        [InlineData("")]
        public void Ip_ShouldAcceptVariousIpFormats(string ipValue)
        {
            // Arrange
            var entity = new UserSessionEntity();

            // Act
            entity.Ip = ipValue;

            // Assert
            Assert.Equal(ipValue, entity.Ip);
        }

        [Fact]
        public void BsonCollectionAttribute_ShouldPointToCorrectCollection()
        {
            // Act
            var collectionName = Document.GetCollectionName(typeof(UserSessionEntity));

            // Assert
            Assert.Equal("usersessions", collectionName);
        }

        [Fact]
        public void Entity_ShouldHaveCorrectPropertyCount()
        {
            // Act
            var allProperties = typeof(UserSessionEntity).GetProperties();
            var entitySpecificProperties = typeof(UserSessionEntity).GetProperties()
                .Where(p => p.DeclaringType == typeof(UserSessionEntity));

            // Assert - Entity has its own properties plus inherited from Document
            Assert.True(allProperties.Length > 5); // Including inherited properties
            Assert.Equal(5, entitySpecificProperties.Count()); // Only entity-specific properties
        }

        [Fact]
        public void Entity_ShouldBePublicClass()
        {
            // Act
            var type = typeof(UserSessionEntity);

            // Assert
            Assert.True(type.IsPublic);
            Assert.True(type.IsClass);
            Assert.False(type.IsAbstract);
            Assert.False(type.IsSealed);
        }

        [Fact]
        public void BsonElementAttributes_ShouldHaveCorrectElementNames()
        {
            // Act & Assert - Verify all BSON element names match MongoDB convention
            var properties = new Dictionary<string, string>
            {
                { nameof(UserSessionEntity.RecheckInTime), "recheckInTime" },
                { nameof(UserSessionEntity.ParnerId), "partnerId" },
                { nameof(UserSessionEntity.BrowserUserAgent), "browserUserAgent" },
                { nameof(UserSessionEntity.PlayerId), "playerId" },
                { nameof(UserSessionEntity.Ip), "ip" }
            };

            foreach (var kvp in properties)
            {
                var property = typeof(UserSessionEntity).GetProperty(kvp.Key);
                var attribute = property!.GetCustomAttribute<BsonElementAttribute>();
                
                Assert.NotNull(attribute);
                Assert.Equal(kvp.Value, attribute.ElementName);
            }
        }
    }
}