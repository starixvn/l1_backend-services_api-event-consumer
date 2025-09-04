using ApiEventConsumer.Core.Models.Promotion;

namespace ApiEventConsumer.Database.Tests.Models
{
    public class HasBonusWinResponseTests
    {
        [Fact]
        public void Constructor_ShouldCreateInstance()
        {
            // Act
            var response = new HasBonusWinResponse();

            // Assert
            Assert.IsType<HasBonusWinResponse>(response);
        }

        [Fact]
        public void Properties_ShouldHaveCorrectDefaultValues()
        {
            // Act
            var response = new HasBonusWinResponse();

            // Assert
            Assert.False(response.Success);
            Assert.False(response.HasBonusWin);
            Assert.Equal(string.Empty, response.Code);
            Assert.Equal(string.Empty, response.Message);
        }

        [Fact]
        public void Success_ShouldSetAndGetValue()
        {
            // Arrange
            var response = new HasBonusWinResponse
            {
                Success = true
            };

            // Assert
            Assert.True(response.Success);

            // Act
            response.Success = false;

            // Assert
            Assert.False(response.Success);
        }

        [Fact]
        public void HasBonusWin_ShouldSetAndGetValue()
        {
            // Arrange
            var response = new HasBonusWinResponse
            {
                HasBonusWin = true
            };

            // Assert
            Assert.True(response.HasBonusWin);

            // Act
            response.HasBonusWin = false;

            // Assert
            Assert.False(response.HasBonusWin);
        }

        [Fact]
        public void Code_ShouldSetAndGetValue()
        {
            // Arrange
            var response = new HasBonusWinResponse();
            const string testCode = "BONUS_WIN_001";

            // Act
            response.Code = testCode;

            // Assert
            Assert.Equal(testCode, response.Code);
        }

        [Fact]
        public void Message_ShouldSetAndGetValue()
        {
            // Arrange
            var response = new HasBonusWinResponse();
            const string testMessage = "Bonus win detected for player";

            // Act
            response.Message = testMessage;

            // Assert
            Assert.Equal(testMessage, response.Message);
        }

        [Fact]
        public void AllProperties_ShouldHaveCorrectTypes()
        {
            // Act
            var successProperty = typeof(HasBonusWinResponse).GetProperty(nameof(HasBonusWinResponse.Success));
            var hasBonusWinProperty = typeof(HasBonusWinResponse).GetProperty(nameof(HasBonusWinResponse.HasBonusWin));
            var codeProperty = typeof(HasBonusWinResponse).GetProperty(nameof(HasBonusWinResponse.Code));
            var messageProperty = typeof(HasBonusWinResponse).GetProperty(nameof(HasBonusWinResponse.Message));

            // Assert
            Assert.Equal(typeof(bool), successProperty!.PropertyType);
            Assert.Equal(typeof(bool), hasBonusWinProperty!.PropertyType);
            Assert.Equal(typeof(string), codeProperty!.PropertyType);
            Assert.Equal(typeof(string), messageProperty!.PropertyType);
        }

        [Fact]
        public void Response_WithCompleteData_ShouldSetAllProperties()
        {
            // Arrange
            const bool testSuccess = true;
            const bool testHasBonusWin = true;
            const string testCode = "SUCCESS_001";
            const string testMessage = "Bonus win successfully processed";

            // Act
            var response = new HasBonusWinResponse
            {
                Success = testSuccess,
                HasBonusWin = testHasBonusWin,
                Code = testCode,
                Message = testMessage
            };

            // Assert
            Assert.Equal(testSuccess, response.Success);
            Assert.Equal(testHasBonusWin, response.HasBonusWin);
            Assert.Equal(testCode, response.Code);
            Assert.Equal(testMessage, response.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("ERROR_001")]
        [InlineData("SUCCESS_999")]
        [InlineData("SPECIAL_CHARS_!@#$%")]
        public void Code_ShouldAcceptVariousStringValues(string codeValue)
        {
            // Arrange & Act
            var response = new HasBonusWinResponse
            {
                Code = codeValue
            };

            // Assert
            Assert.Equal(codeValue, response.Code);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("Short message")]
        [InlineData("This is a very long message that could contain detailed information about the bonus win status")]
        [InlineData("Message with special chars: !@#$%^&*()")]
        public void Message_ShouldAcceptVariousStringValues(string messageValue)
        {
            // Arrange & Act
            var response = new HasBonusWinResponse
            {
                Message = messageValue
            };

            // Assert
            Assert.Equal(messageValue, response.Message);
        }

        [Fact]
        public void Properties_ShouldBeReadWritable()
        {
            // Act
            var successProperty = typeof(HasBonusWinResponse).GetProperty(nameof(HasBonusWinResponse.Success));
            var hasBonusWinProperty = typeof(HasBonusWinResponse).GetProperty(nameof(HasBonusWinResponse.HasBonusWin));
            var codeProperty = typeof(HasBonusWinResponse).GetProperty(nameof(HasBonusWinResponse.Code));
            var messageProperty = typeof(HasBonusWinResponse).GetProperty(nameof(HasBonusWinResponse.Message));

            // Assert - All properties should be readable and writable
            Assert.True(successProperty!.CanRead);
            Assert.True(successProperty.CanWrite);
            Assert.True(hasBonusWinProperty!.CanRead);
            Assert.True(hasBonusWinProperty.CanWrite);
            Assert.True(codeProperty!.CanRead);
            Assert.True(codeProperty.CanWrite);
            Assert.True(messageProperty!.CanRead);
            Assert.True(messageProperty.CanWrite);
        }

        [Fact]
        public void Response_ShouldHaveCorrectPropertyCount()
        {
            // Act
            var properties = typeof(HasBonusWinResponse).GetProperties();

            // Assert
            Assert.Equal(4, properties.Length);
        }

        [Fact]
        public void Response_ShouldHaveAllExpectedProperties()
        {
            // Act
            var propertyNames = typeof(HasBonusWinResponse).GetProperties()
                .Select(p => p.Name)
                .ToList();

            // Assert
            Assert.Contains("Success", propertyNames);
            Assert.Contains("HasBonusWin", propertyNames);
            Assert.Contains("Code", propertyNames);
            Assert.Contains("Message", propertyNames);
        }

        [Fact]
        public void Class_ShouldBePublicClass()
        {
            // Act
            var type = typeof(HasBonusWinResponse);

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
            var response = new HasBonusWinResponse
            {
                Code = null!,
                Message = null!
            };

            Assert.Null(response.Code);
            Assert.Null(response.Message);
        }

        [Fact]
        public void Response_SuccessScenario_ShouldRepresentValidState()
        {
            // Arrange & Act
            var successResponse = new HasBonusWinResponse
            {
                Success = true,
                HasBonusWin = true,
                Code = "BONUS_WIN_SUCCESS",
                Message = "Player has bonus win available"
            };

            // Assert
            Assert.True(successResponse.Success);
            Assert.True(successResponse.HasBonusWin);
            Assert.Equal("BONUS_WIN_SUCCESS", successResponse.Code);
            Assert.Equal("Player has bonus win available", successResponse.Message);
        }

        [Fact]
        public void Response_ErrorScenario_ShouldRepresentValidState()
        {
            // Arrange & Act
            var errorResponse = new HasBonusWinResponse
            {
                Success = false,
                HasBonusWin = false,
                Code = "ERROR_404",
                Message = "Player not found"
            };

            // Assert
            Assert.False(errorResponse.Success);
            Assert.False(errorResponse.HasBonusWin);
            Assert.Equal("ERROR_404", errorResponse.Code);
            Assert.Equal("Player not found", errorResponse.Message);
        }
    }
}
