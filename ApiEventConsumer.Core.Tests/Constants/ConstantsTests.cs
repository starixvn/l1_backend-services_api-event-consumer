using ApiEventConsumer.Core.Constants;

namespace ApiEventConsumer.Core.Tests.Constants
{
    public class ConstantsTests
    {
        [Fact]
        public void SystemType_Constants_ShouldHaveCorrectValues()
        {
            // Assert
            Assert.Equal("SC1", SystemType.SC1);
            Assert.Equal("VG2", SystemType.VG2);
            Assert.Equal("GN", SystemType.GN);
        }

        [Theory]
        [InlineData("SC1")]
        [InlineData("VG2")]
        [InlineData("GN")]
        public void SystemType_Constants_ShouldBeNonEmptyStrings(string systemType)
        {
            // Assert
            Assert.False(string.IsNullOrEmpty(systemType));
        }
    }
}
