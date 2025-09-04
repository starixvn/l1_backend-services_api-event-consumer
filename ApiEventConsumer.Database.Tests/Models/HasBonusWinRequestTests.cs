using ApiEventConsumer.Core.Models.Promotion;

namespace ApiEventConsumer.Database.Tests.Models
{
    public class HasBonusWinRequestTests
    {
        [Fact]
        public void Constructor_ShouldCreateInstance()
        {
            // Act
            var request = new HasBonusWinRequest();

            // Assert
            Assert.IsType<HasBonusWinRequest>(request);
        }

        [Fact]
        public void Properties_ShouldHaveCorrectDefaultValues()
        {
            // Act
            var request = new HasBonusWinRequest();

            // Assert
            Assert.Equal(string.Empty, request.PlayerId);
            Assert.Equal(string.Empty, request.OperatorId);
            Assert.Equal(string.Empty, request.SiteName);
        }

        [Fact]
        public void PlayerId_ShouldSetAndGetValue()
        {
            // Arrange
            var request = new HasBonusWinRequest();
            const string testPlayerId = "player-12345";

            // Act
            request.PlayerId = testPlayerId;

            // Assert
            Assert.Equal(testPlayerId, request.PlayerId);
        }

        [Fact]
        public void OperatorId_ShouldSetAndGetValue()
        {
            // Arrange
            var request = new HasBonusWinRequest();
            const string testOperatorId = "operator-67890";

            // Act
            request.OperatorId = testOperatorId;

            // Assert
            Assert.Equal(testOperatorId, request.OperatorId);
        }

        [Fact]
        public void SiteName_ShouldSetAndGetValue()
        {
            // Arrange
            var request = new HasBonusWinRequest();
            const string testSiteName = "casino-site.com";

            // Act
            request.SiteName = testSiteName;

            // Assert
            Assert.Equal(testSiteName, request.SiteName);
        }

        [Fact]
        public void AllProperties_ShouldHaveCorrectTypes()
        {
            // Act
            var playerIdProperty = typeof(HasBonusWinRequest).GetProperty(nameof(HasBonusWinRequest.PlayerId));
            var operatorIdProperty = typeof(HasBonusWinRequest).GetProperty(nameof(HasBonusWinRequest.OperatorId));
            var siteNameProperty = typeof(HasBonusWinRequest).GetProperty(nameof(HasBonusWinRequest.SiteName));

            // Assert
            Assert.Equal(typeof(string), playerIdProperty!.PropertyType);
            Assert.Equal(typeof(string), operatorIdProperty!.PropertyType);
            Assert.Equal(typeof(string), siteNameProperty!.PropertyType);
        }

        [Fact]
        public void Request_WithCompleteData_ShouldSetAllProperties()
        {
            // Arrange
            const string testPlayerId = "player-999";
            const string testOperatorId = "op-001";
            const string testSiteName = "bonus-casino.net";

            // Act
            var request = new HasBonusWinRequest
            {
                PlayerId = testPlayerId,
                OperatorId = testOperatorId,
                SiteName = testSiteName
            };

            // Assert
            Assert.Equal(testPlayerId, request.PlayerId);
            Assert.Equal(testOperatorId, request.OperatorId);
            Assert.Equal(testSiteName, request.SiteName);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("player-001")]
        [InlineData("user_12345")]
        [InlineData("PLAYER-WITH-SPECIAL@CHARS")]
        public void PlayerId_ShouldAcceptVariousStringValues(string playerIdValue)
        {
            // Arrange & Act
            var request = new HasBonusWinRequest
            {
                PlayerId = playerIdValue
            };

            // Assert
            Assert.Equal(playerIdValue, request.PlayerId);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("OP001")]
        [InlineData("operator-beta")]
        [InlineData("OP_SPECIAL_!@#")]
        public void OperatorId_ShouldAcceptVariousStringValues(string operatorIdValue)
        {
            // Arrange & Act
            var request = new HasBonusWinRequest
            {
                OperatorId = operatorIdValue
            };

            // Assert
            Assert.Equal(operatorIdValue, request.OperatorId);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("site.com")]
        [InlineData("https://my-casino-site.net")]
        [InlineData("Site Name With Spaces & Special Chars!")]
        public void SiteName_ShouldAcceptVariousStringValues(string siteNameValue)
        {
            // Arrange & Act
            var request = new HasBonusWinRequest
            {
                SiteName = siteNameValue
            };

            // Assert
            Assert.Equal(siteNameValue, request.SiteName);
        }

        [Fact]
        public void Properties_ShouldBeReadWritable()
        {
            // Act
            var playerIdProperty = typeof(HasBonusWinRequest).GetProperty(nameof(HasBonusWinRequest.PlayerId));
            var operatorIdProperty = typeof(HasBonusWinRequest).GetProperty(nameof(HasBonusWinRequest.OperatorId));
            var siteNameProperty = typeof(HasBonusWinRequest).GetProperty(nameof(HasBonusWinRequest.SiteName));

            // Assert - All properties should be readable and writable
            Assert.True(playerIdProperty!.CanRead);
            Assert.True(playerIdProperty.CanWrite);
            Assert.True(operatorIdProperty!.CanRead);
            Assert.True(operatorIdProperty.CanWrite);
            Assert.True(siteNameProperty!.CanRead);
            Assert.True(siteNameProperty.CanWrite);
        }

        [Fact]
        public void Request_ShouldHaveCorrectPropertyCount()
        {
            // Act
            var properties = typeof(HasBonusWinRequest).GetProperties();

            // Assert
            Assert.Equal(3, properties.Length);
        }

        [Fact]
        public void Request_ShouldHaveAllExpectedProperties()
        {
            // Act
            var propertyNames = typeof(HasBonusWinRequest).GetProperties()
                .Select(p => p.Name)
                .ToList();

            // Assert
            Assert.Contains("PlayerId", propertyNames);
            Assert.Contains("OperatorId", propertyNames);
            Assert.Contains("SiteName", propertyNames);
        }

        [Fact]
        public void Class_ShouldBePublicClass()
        {
            // Act
            var type = typeof(HasBonusWinRequest);

            // Assert
            Assert.True(type.IsPublic);
            Assert.True(type.IsClass);
            Assert.False(type.IsAbstract);
            Assert.False(type.IsSealed);
        }

        [Fact]
        public void StringProperties_ShouldHandleNullValues()
        {
            // Arrange & Act - Should handle null assignment gracefully
            var request = new HasBonusWinRequest
            {
                PlayerId = null!,
                OperatorId = null!,
                SiteName = null!
            };

            // Assert
            Assert.Null(request.PlayerId);
            Assert.Null(request.OperatorId);
            Assert.Null(request.SiteName);
        }

        [Fact]
        public void Request_ValidScenario_ShouldRepresentValidState()
        {
            // Arrange & Act
            var validRequest = new HasBonusWinRequest
            {
                PlayerId = "player-777",
                OperatorId = "casino-operator-01",
                SiteName = "lucky-casino.com"
            };

            // Assert
            Assert.Equal("player-777", validRequest.PlayerId);
            Assert.Equal("casino-operator-01", validRequest.OperatorId);
            Assert.Equal("lucky-casino.com", validRequest.SiteName);
        }

        [Fact]
        public void Request_EmptyScenario_ShouldRepresentValidState()
        {
            // Arrange & Act
            var emptyRequest = new HasBonusWinRequest
            {
                PlayerId = "",
                OperatorId = "",
                SiteName = ""
            };

            // Assert
            Assert.Equal("", emptyRequest.PlayerId);
            Assert.Equal("", emptyRequest.OperatorId);
            Assert.Equal("", emptyRequest.SiteName);
        }

        [Fact]
        public void Request_PropertyCount_ShouldBeExactlyThree()
        {
            // Act
            var allProperties = typeof(HasBonusWinRequest).GetProperties();

            // Assert - Should have exactly 3 properties
            Assert.Equal(3, allProperties.Length);
        }

        [Fact]
        public void Request_ClassType_ShouldBePublicAndNotAbstract()
        {
            // Act
            var type = typeof(HasBonusWinRequest);

            // Assert
            Assert.True(type.IsPublic);
            Assert.True(type.IsClass);
            Assert.False(type.IsAbstract);
            Assert.False(type.IsSealed);
        }

        [Fact]
        public void DefaultConstructor_ShouldInitializeAllPropertiesWithEmptyStrings()
        {
            // Act
            var request = new HasBonusWinRequest();

            // Assert - All string properties should default to empty string
            Assert.Equal(string.Empty, request.PlayerId);
            Assert.Equal(string.Empty, request.OperatorId);
            Assert.Equal(string.Empty, request.SiteName);
            
            // Verify none are null
            Assert.NotNull(request.PlayerId);
            Assert.NotNull(request.OperatorId);
            Assert.NotNull(request.SiteName);
        }

        [Fact]
        public void Request_WithMixedData_ShouldMaintainIndependentProperties()
        {
            // Arrange & Act
            var request = new HasBonusWinRequest
            {
                PlayerId = "test-player",
                OperatorId = "", // Empty
                SiteName = "test-site"
            };

            // Assert - Each property should maintain its own value independently
            Assert.Equal("test-player", request.PlayerId);
            Assert.Equal("", request.OperatorId);
            Assert.Equal("test-site", request.SiteName);
        }
    }
}
