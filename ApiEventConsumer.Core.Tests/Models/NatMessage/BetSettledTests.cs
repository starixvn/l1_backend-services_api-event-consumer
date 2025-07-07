using ApiEventConsumer.Core.Models.NatMessage;

namespace ApiEventConsumer.Core.Tests.Models.NatMessage
{
    public class BetSettledTests
    {
        [Fact]
        public void DefaultConstructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var betSettled = new BetSettled();

            // Assert
            Assert.Equal(0, betSettled.GameRoundId);
            Assert.Equal(0, betSettled.GameTypeId);
            Assert.Equal(0m, betSettled.TotalAmount);
            Assert.Equal(0m, betSettled.TotalWin);
            Assert.Equal(0m, betSettled.ValidBetAmount);
            Assert.Null(betSettled.EndTime);
            Assert.False(betSettled.FreeBet);
            Assert.Equal(string.Empty, betSettled.PromotionId);
            Assert.Equal(string.Empty, betSettled.Seq);
            Assert.Equal(string.Empty, betSettled.PlayerId);
            Assert.Equal(string.Empty, betSettled.SiteId);
            Assert.Equal(string.Empty, betSettled.OperatorId);
            Assert.Equal(string.Empty, betSettled.SiteName);
            Assert.Equal(default, betSettled.TimeStamp);
        }

        [Fact]
        public void Properties_ShouldSetAndGetCorrectly()
        {
            // Arrange
            var now = DateTimeOffset.UtcNow;
            var endTime = now.AddHours(1);

            var betSettled = new BetSettled
            {
                GameRoundId = 1,
                GameTypeId = 2,
                TotalAmount = 100.50m,
                TotalWin = 200.00m,
                ValidBetAmount = 95.00m,
                EndTime = endTime,
                FreeBet = true,
                PromotionId = "promo_123",
                Seq = "seq_123",
                PlayerId = 456,
                SiteId = "site_123",
                OperatorId = "op_123",
                SiteName = "site_name_123"
            };

            // Assert
            Assert.Equal(1, betSettled.GameRoundId);
            Assert.Equal(2, betSettled.GameTypeId);
            Assert.Equal(100.50m, betSettled.TotalAmount);
            Assert.Equal(200.00m, betSettled.TotalWin);
            Assert.Equal(95.00m, betSettled.ValidBetAmount);
            Assert.Equal(endTime, betSettled.EndTime);
            Assert.True(betSettled.FreeBet);
            Assert.Equal("promo_123", betSettled.PromotionId);
            Assert.Equal("seq_123", betSettled.Seq);
            Assert.Equal(456, betSettled.PlayerId);
            Assert.Equal("site_123", betSettled.SiteId);
            Assert.Equal("op_123", betSettled.OperatorId);
            Assert.Equal("site_name_123", betSettled.SiteName);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MaxValue)]
        public void NumericProperties_ShouldHandleEdgeCases(int value)
        {
            // Arrange
            var betSettled = new BetSettled
            {
                GameRoundId = value,
                GameTypeId = value,
                PlayerId = value
            };

            // Assert
            Assert.Equal(value, betSettled.GameRoundId);
            Assert.Equal(value, betSettled.GameTypeId);
            Assert.Equal(value, betSettled.PlayerId);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(-1.0)]
        [InlineData(999999.99)]
        public void DecimalProperties_ShouldHandleEdgeCases(double value)
        {
            // Arrange
            var decimalValue = (decimal)value;
            var betSettled = new BetSettled
            {
                TotalAmount = decimalValue,
                TotalWin = decimalValue,
                ValidBetAmount = decimalValue
            };

            // Assert
            Assert.Equal(decimalValue, betSettled.TotalAmount);
            Assert.Equal(decimalValue, betSettled.TotalWin);
            Assert.Equal(decimalValue, betSettled.ValidBetAmount);
        }

        [Fact]
        public void EndTime_ShouldHandleNullValue()
        {
            // Arrange
            var betSettled = new BetSettled();

            // Assert - Null EndTime
            Assert.Null(betSettled.EndTime);

            // Act - Set EndTime
            var endTime = DateTimeOffset.UtcNow;
            betSettled.EndTime = endTime;

            // Assert - EndTime is set
            Assert.Equal(endTime, betSettled.EndTime);
        }

        [Theory]
        [InlineData("promo_123")]
        [InlineData("")]
        [InlineData(null)]
        public void PromotionId_ShouldHandleValidValues(string promotionId)
        {
            // Arrange
            var betSettled = new BetSettled
            {
                PromotionId = promotionId
            };

            // Assert
            Assert.Equal(promotionId, betSettled.PromotionId);
        }

        [Theory]
        [InlineData("seq_123")]
        [InlineData("")]
        [InlineData(null)]
        public void Seq_ShouldHandleValidValues(string seq)
        {
            // Arrange
            var betSettled = new BetSettled
            {
                Seq = seq
            };

            // Assert
            Assert.Equal(seq, betSettled.Seq);
        }

        [Theory]
        [InlineData("site_123")]
        [InlineData("")]
        [InlineData(null)]
        public void SiteId_ShouldHandleValidValues(string siteId)
        {
            // Arrange
            var betSettled = new BetSettled
            {
                SiteId = siteId
            };

            // Assert
            Assert.Equal(siteId, betSettled.SiteId);
        }

        [Theory]
        [InlineData("op_123")]
        [InlineData("")]
        [InlineData(null)]
        public void OperatorId_ShouldHandleValidValues(string operatorId)
        {
            // Arrange
            var betSettled = new BetSettled
            {
                OperatorId = operatorId
            };

            // Assert
            Assert.Equal(operatorId, betSettled.OperatorId);
        }

        [Theory]
        [InlineData("site_name_123")]
        [InlineData("")]
        [InlineData(null)]
        public void SiteName_ShouldHandleValidValues(string siteName)
        {
            // Arrange
            var betSettled = new BetSettled
            {
                SiteName = siteName
            };

            // Assert
            Assert.Equal(siteName, betSettled.SiteName);
        }
    }
} 