using ApiEventConsumer.Core.Models.ChatBot;

namespace ApiEventConsumer.Core.Tests.Models.ChatBot
{
    public class ChatBotResultTests
    {
        [Fact]
        public void DefaultConstructor_ShouldSetDefaultValues()
        {
            // Act
            var result = new ChatBotResult();

            // Assert
            Assert.True(result.Success);
            Assert.Equal(string.Empty, result.Message);
        }

        [Fact]
        public void Properties_ShouldSetAndGetCorrectly()
        {
            // Arrange
            var result = new ChatBotResult
            {
                Success = false,
                Message = "Test message"
            };

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Test message", result.Message);
        }

        [Theory]
        [InlineData(true, "Success message")]
        [InlineData(false, "Error message")]
        [InlineData(true, "")]
        [InlineData(false, null)]
        public void Properties_ShouldHandleVariousValues(bool success, string? message)
        {
            // Arrange
            var result = new ChatBotResult
            {
                Success = success,
                Message = message ?? string.Empty
            };

            // Assert
            Assert.Equal(success, result.Success);
            Assert.Equal(message ?? string.Empty, result.Message);
        }
    }
}
