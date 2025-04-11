namespace ApiEventConsumer.Core.Tests.Enumeration
{
    public class TestEnumeration : Core.Enumeration.Enumeration
    {
        public static readonly TestEnumeration One = new("1", "One");
        public static readonly TestEnumeration Two = new("2", "Two");
        public static readonly TestEnumeration Three = new("3", "Three");

        public TestEnumeration() : base("default", "Default") { }
        private TestEnumeration(string value, string displayName) : base(value, displayName) { }
    }

    public class EnumerationTests
    {
        [Fact]
        public void Constructor_ShouldSetProperties()
        {
            // Arrange & Act
            var enumeration = new TestEnumeration();

            // Assert
            Assert.Equal("default", enumeration.Value);
            Assert.Equal("Default", enumeration.DisplayName);
        }

        [Fact]
        public void GetAll_ShouldReturnAllDefinedValues()
        {
            // Act
            var values = Core.Enumeration.Enumeration.GetAll<TestEnumeration>().ToList();

            // Assert
            Assert.Equal(3, values.Count);
            Assert.Contains(values, v => v.Value == "1" && v.DisplayName == "One");
            Assert.Contains(values, v => v.Value == "2" && v.DisplayName == "Two");
            Assert.Contains(values, v => v.Value == "3" && v.DisplayName == "Three");
        }

        [Fact]
        public void GetAll_WithType_ShouldReturnAllDefinedValues()
        {
            // Act
            var values = ApiEventConsumer.Core.Enumeration.Enumeration.GetAll(typeof(TestEnumeration)).Cast<TestEnumeration>().ToList();

            // Assert
            Assert.Equal(3, values.Count);
            Assert.Contains(values, v => v.Value == "1" && v.DisplayName == "One");
            Assert.Contains(values, v => v.Value == "2" && v.DisplayName == "Two");
            Assert.Contains(values, v => v.Value == "3" && v.DisplayName == "Three");
        }

        [Fact]
        public void EqualityOperator_WithNullValues_ShouldWorkCorrectly()
        {
            // Arrange
            TestEnumeration? nullEnum = null;
            var nonNullEnum = TestEnumeration.One;

            // Assert
            Assert.True(nullEnum == null);
            Assert.False(nullEnum == nonNullEnum);
            Assert.False(nonNullEnum == nullEnum);
            Assert.True(nullEnum != nonNullEnum);
            Assert.True(nonNullEnum != nullEnum);
        }

        [Fact]
        public void ComparisonOperators_WithNullValues_ShouldWorkCorrectly()
        {
            // Arrange
            TestEnumeration? nullEnum = null;
            var one = TestEnumeration.One;
            var two = TestEnumeration.Two;

            // Assert
            Assert.True(nullEnum < one);
            Assert.True(nullEnum <= one);
            Assert.False(nullEnum > one);
            Assert.False(one < nullEnum);
            Assert.False(one <= nullEnum);
            Assert.True(one > nullEnum);
            Assert.True(one >= nullEnum);
            Assert.True(nullEnum <= nullEnum);
            Assert.True(nullEnum >= nullEnum);
        }

        [Fact]
        public void CompareTo_WithNullValue_ShouldThrowArgumentNullException()
        {
            // Arrange
            var enumeration = TestEnumeration.One;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => enumeration.CompareTo(null));
        }

        [Fact]
        public void FromDisplayName_WithInvalidDisplayName_ShouldThrowException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => 
                ApiEventConsumer.Core.Enumeration.Enumeration.FromDisplayName<TestEnumeration>("InvalidDisplayName"));
            
            Assert.Contains("is not a valid display name", exception.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void Parse_WithInvalidValue_ShouldThrowException(string invalidValue)
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => 
                ApiEventConsumer.Core.Enumeration.Enumeration.FromValue<TestEnumeration>(invalidValue));
        }

        [Fact]
        public void Contains_WithNullOrEmptyValue_ShouldReturnFalse()
        {
            // Act & Assert
            Assert.False(ApiEventConsumer.Core.Enumeration.Enumeration.Contains<TestEnumeration>(null));
            Assert.False(ApiEventConsumer.Core.Enumeration.Enumeration.Contains<TestEnumeration>(""));
            Assert.False(ApiEventConsumer.Core.Enumeration.Enumeration.Contains<TestEnumeration>(" "));
        }

        [Fact]
        public void Equals_WithDifferentTypes_ShouldReturnFalse()
        {
            // Arrange
            var enumeration = TestEnumeration.One;
            var differentObject = new object();

            // Act & Assert
            Assert.False(enumeration.Equals(differentObject));
        }

        // Thêm một test class mới để test so sánh giữa các loại Enumeration khác nhau
        public class AnotherTestEnumeration : ApiEventConsumer.Core.Enumeration.Enumeration
        {
            public static readonly AnotherTestEnumeration One = new("1", "One");
            
            public AnotherTestEnumeration() : base("default", "Default") { }
            private AnotherTestEnumeration(string value, string displayName) : base(value, displayName) { }
        }

        [Fact]
        public void Equals_WithDifferentEnumerationTypes_ShouldReturnFalse()
        {
            // Arrange
            var testEnum = TestEnumeration.One;
            var anotherTestEnum = AnotherTestEnumeration.One;

            // Act & Assert
            Assert.False(testEnum.Equals(anotherTestEnum));
        }
    }
}
