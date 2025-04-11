using ApiEventConsumer.Core.Models.NatMessage;

namespace ApiEventConsumer.Core.Tests.Models.NatMessage
{
    public class BetCreatedTests
    {
        [Fact]
        public void DefaultConstructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var betCreated = new BetCreated();

            // Assert
            Assert.Equal(0, betCreated.GameRoundId);
            Assert.Equal(0, betCreated.GameTypeId);
            Assert.Equal(0, betCreated.ChoiceId);
            Assert.Equal(0m, betCreated.Amount);
            Assert.Null(betCreated.Currency);
            Assert.Null(betCreated.RoundEndTime);
            Assert.Null(betCreated.Ip);
            Assert.False(betCreated.FreeSpin);
            Assert.Equal(0m, betCreated.ContributionAmount);
            Assert.Equal(0m, betCreated.ContributionRate);
            Assert.Null(betCreated.OperatorToken);
            Assert.False(betCreated.FreeBet);
            Assert.Null(betCreated.OperatorId);
            Assert.Null(betCreated.UserId);
            Assert.Null(betCreated.Seq);
            Assert.Null(betCreated.PlayerId);
            Assert.Null(betCreated.SiteId);
            Assert.Equal(default, betCreated.TimeStamp);
            Assert.Equal(0, betCreated.System);
            Assert.Null(betCreated.Metadata);
        }

        [Fact]
        public void Properties_ShouldSetAndGetCorrectly()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var roundEndTime = now.AddHours(1);
            var metadata = new Dictionary<string, string> { { "key1", "value1" }, { "key2", "value2" } };

            var betCreated = new BetCreated
            {
                GameRoundId = 1,
                GameTypeId = 2,
                ChoiceId = 3,
                Amount = 100.50m,
                Currency = "USD",
                RoundEndTime = roundEndTime,
                Ip = "192.168.1.1",
                FreeSpin = true,
                ContributionAmount = 10.5m,
                ContributionRate = 0.1m,
                OperatorToken = "op_token_123",
                FreeBet = true,
                OperatorId = "operator_123",
                UserId = "user_123",
                Seq = "seq_123",
                PlayerId = 456,
                SiteId = "site_123",
                TimeStamp = now,
                System = 1,
                Metadata = metadata
            };

            // Assert
            Assert.Equal(1, betCreated.GameRoundId);
            Assert.Equal(2, betCreated.GameTypeId);
            Assert.Equal(3, betCreated.ChoiceId);
            Assert.Equal(100.50m, betCreated.Amount);
            Assert.Equal("USD", betCreated.Currency);
            Assert.Equal(roundEndTime, betCreated.RoundEndTime);
            Assert.Equal("192.168.1.1", betCreated.Ip);
            Assert.True(betCreated.FreeSpin);
            Assert.Equal(10.5m, betCreated.ContributionAmount);
            Assert.Equal(0.1m, betCreated.ContributionRate);
            Assert.Equal("op_token_123", betCreated.OperatorToken);
            Assert.True(betCreated.FreeBet);
            Assert.Equal("operator_123", betCreated.OperatorId);
            Assert.Equal("user_123", betCreated.UserId);
            Assert.Equal("seq_123", betCreated.Seq);
            Assert.Equal(456, betCreated.PlayerId);
            Assert.Equal("site_123", betCreated.SiteId);
            Assert.Equal(now, betCreated.TimeStamp);
            Assert.Equal(1, betCreated.System);
            Assert.Same(metadata, betCreated.Metadata);
            Assert.Equal(2, betCreated.Metadata.Count);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MaxValue)]
        public void NumericProperties_ShouldHandleEdgeCases(int value)
        {
            // Arrange
            var betCreated = new BetCreated
            {
                GameRoundId = value,
                GameTypeId = value,
                ChoiceId = value,
                PlayerId = value,
                System = value
            };

            // Assert
            Assert.Equal(value, betCreated.GameRoundId);
            Assert.Equal(value, betCreated.GameTypeId);
            Assert.Equal(value, betCreated.ChoiceId);
            Assert.Equal(value, betCreated.PlayerId);
            Assert.Equal(value, betCreated.System);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(-1.0)]
        [InlineData(999999.99)]
        public void DecimalProperties_ShouldHandleEdgeCases(double value)
        {
            // Arrange
            var decimalValue = (decimal)value;
            var betCreated = new BetCreated
            {
                Amount = decimalValue,
                ContributionAmount = decimalValue,
                ContributionRate = decimalValue
            };

            // Assert
            Assert.Equal(decimalValue, betCreated.Amount);
            Assert.Equal(decimalValue, betCreated.ContributionAmount);
            Assert.Equal(decimalValue, betCreated.ContributionRate);
        }

        [Fact]
        public void Metadata_ShouldHandleNullAndEmptyDictionary()
        {
            // Arrange
            var betCreated = new BetCreated();

            // Assert - Null dictionary
            Assert.Null(betCreated.Metadata);

            // Act - Empty dictionary
            betCreated.Metadata = new Dictionary<string, string>();

            // Assert - Empty dictionary
            Assert.NotNull(betCreated.Metadata);
            Assert.Empty(betCreated.Metadata);
        }
    }
}
