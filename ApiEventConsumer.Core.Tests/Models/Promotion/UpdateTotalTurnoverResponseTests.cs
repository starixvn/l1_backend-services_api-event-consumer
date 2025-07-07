using ApiEventConsumer.Core.Models.Promotion;
using Xunit;

namespace ApiEventConsumer.Core.Tests.Models.Promotion
{
    public class UpdateTotalTurnoverResponseTests
    {
        [Fact]
        public void UpdateTotalTurnoverResponse_DefaultValues_ShouldBeCorrect()
        {
            var response = new UpdateTotalTurnoverResponse();

            Assert.False(response.Success);
            Assert.Equal(0m, response.TotalTurnover);
            Assert.Equal(0m, response.RemainingTO);
            Assert.Equal(string.Empty, response.Code);
            Assert.Equal(string.Empty, response.Message);
        }

        [Fact]
        public void UpdateTotalTurnoverResponse_SetProperties_ShouldReturnCorrectValues()
        {
            var response = new UpdateTotalTurnoverResponse
            {
                Success = true,
                TotalTurnover = 250.75m,
                RemainingTO = 749.25m,
                Code = "SUCCESS",
                Message = "Operation completed successfully"
            };

            Assert.True(response.Success);
            Assert.Equal(250.75m, response.TotalTurnover);
            Assert.Equal(749.25m, response.RemainingTO);
            Assert.Equal("SUCCESS", response.Code);
            Assert.Equal("Operation completed successfully", response.Message);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Success_ShouldHandleValidValues(bool success)
        {
            var response = new UpdateTotalTurnoverResponse
            {
                Success = success
            };

            Assert.Equal(success, response.Success);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(100.50)]
        [InlineData(999.99)]
        public void TotalTurnover_ShouldHandleValidValues(decimal totalTurnover)
        {
            var response = new UpdateTotalTurnoverResponse
            {
                TotalTurnover = totalTurnover
            };

            Assert.Equal(totalTurnover, response.TotalTurnover);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(50.25)]
        [InlineData(1000)]
        public void RemainingTO_ShouldHandleValidValues(decimal remainingTO)
        {
            // Arrange
            var response = new UpdateTotalTurnoverResponse
            {
                RemainingTO = remainingTO
            };

            // Assert
            Assert.Equal(remainingTO, response.RemainingTO);
        }

        [Theory]
        [InlineData("SUCCESS")]
        [InlineData("ERROR")]
        [InlineData("")]
        [InlineData(null)]
        public void Code_ShouldHandleValidValues(string code)
        {
            // Arrange
            var response = new UpdateTotalTurnoverResponse
            {
                Code = code
            };

            // Assert
            Assert.Equal(code, response.Code);
        }

        [Theory]
        [InlineData("Success")]
        [InlineData("Error occurred")]
        [InlineData("")]
        [InlineData(null)]
        public void Message_ShouldHandleValidValues(string message)
        {
            // Arrange
            var response = new UpdateTotalTurnoverResponse
            {
                Message = message
            };

            // Assert
            Assert.Equal(message, response.Message);
        }
    }
} 