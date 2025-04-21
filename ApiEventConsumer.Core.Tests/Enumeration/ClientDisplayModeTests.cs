using ApiEventConsumer.Core.Enumeration;

namespace ApiEventConsumer.Core.Tests.Enumeration
{
    public class ClientDisplayModeTests
    {
        [Fact]
        public void DefaultConstructor_ShouldSetDefaultValues()
        {
            // Act
            var displayMode = new ClientDisplayMode();

            // Assert
            Assert.Equal("default", displayMode.Value);
            Assert.Equal("Default Display Mode", displayMode.DisplayName);
        }

        [Theory]
        [InlineData("test", "Test Display")]
        [InlineData("1", "Landscape")]
        [InlineData("2", "Portrait")]
        public void ParameterizedConstructor_ShouldSetCorrectValues(string value, string displayName)
        {
            // Act
            var displayMode = new ClientDisplayMode(value, displayName);

            // Assert
            Assert.Equal(value, displayMode.Value);
            Assert.Equal(displayName, displayMode.DisplayName);
        }

        [Fact]
        public void Constants_ShouldHaveCorrectValues()
        {
            // Assert
            Assert.Equal("2", ClientDisplayMode.PortraitValue);
            Assert.Equal("1", ClientDisplayMode.LandscapeValue);
        }

        [Fact]
        public void StaticInstances_ShouldHaveCorrectValues()
        {
            // Portrait
            Assert.Equal(ClientDisplayMode.PortraitValue, ClientDisplayMode.Portrait.Value);
            Assert.Equal("Portrait", ClientDisplayMode.Portrait.DisplayName);

            // Landscape
            Assert.Equal(ClientDisplayMode.LandscapeValue, ClientDisplayMode.Landscape.Value);
            Assert.Equal("Landscape", ClientDisplayMode.Landscape.DisplayName);
        }
    }
}
