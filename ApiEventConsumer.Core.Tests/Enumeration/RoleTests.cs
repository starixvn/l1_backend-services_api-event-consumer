using ApiEventConsumer.Core.Enumeration;

namespace ApiEventConsumer.Core.Tests.Enumeration
{
    public class RoleTests
    {
        [Fact]
        public void DefaultConstructor_ShouldSetDefaultValues()
        {
            // Act
            var role = new Role();

            // Assert
            Assert.Equal("default", role.Value);
            Assert.Equal("Default Role", role.DisplayName);
        }

        [Theory]
        [InlineData("test_role", "Test Role")]
        [InlineData("admin", "Admin")]
        [InlineData("operation", "Operation")]
        public void ParameterizedConstructor_ShouldSetCorrectValues(string value, string displayName)
        {
            // Act
            var role = new Role(value, displayName);

            // Assert
            Assert.Equal(value, role.Value);
            Assert.Equal(displayName, role.DisplayName);
        }

        [Fact]
        public void Constants_ShouldHaveCorrectValues()
        {
            // Assert
            Assert.Equal("operation", Role.OperationValue);
            Assert.Equal("admin", Role.AdminValue);
        }

        [Fact]
        public void StaticInstances_ShouldBeInitializedCorrectly()
        {
            // Operation Role
            Assert.NotNull(Role.Operation);
            Assert.Equal(Role.OperationValue, Role.Operation.Value);
            Assert.Equal("Operation", Role.Operation.DisplayName);

            // Admin Role
            Assert.NotNull(Role.Admin);
            Assert.Equal(Role.AdminValue, Role.Admin.Value);
            Assert.Equal("Admin", Role.Admin.DisplayName);
        }

        [Fact]
        public void GetAll_ShouldReturnAllDefinedRoles()
        {
            // Act
            var roles = Core.Enumeration.Enumeration.GetAll<Role>().ToList();

            // Assert
            Assert.Equal(2, roles.Count);
            Assert.Contains(roles, r => r.Value == Role.OperationValue && r.DisplayName == "Operation");
            Assert.Contains(roles, r => r.Value == Role.AdminValue && r.DisplayName == "Admin");
        }

        [Fact]
        public void FromValue_ShouldReturnCorrectRole()
        {
            // Act
            var operationRole = Core.Enumeration.Enumeration.FromValue<Role>(Role.OperationValue);
            var adminRole = Core.Enumeration.Enumeration.FromValue<Role>(Role.AdminValue);

            // Assert
            Assert.Equal(Role.Operation.Value, operationRole.Value);
            Assert.Equal(Role.Operation.DisplayName, operationRole.DisplayName);
            Assert.Equal(Role.Admin.Value, adminRole.Value);
            Assert.Equal(Role.Admin.DisplayName, adminRole.DisplayName);
        }

        [Fact]
        public void FromDisplayName_ShouldReturnCorrectRole()
        {
            // Act
            var operationRole = Core.Enumeration.Enumeration.FromDisplayName<Role>("Operation");
            var adminRole = Core.Enumeration.Enumeration.FromDisplayName<Role>("Admin");

            // Assert
            Assert.Equal(Role.Operation.Value, operationRole.Value);
            Assert.Equal(Role.Operation.DisplayName, operationRole.DisplayName);
            Assert.Equal(Role.Admin.Value, adminRole.Value);
            Assert.Equal(Role.Admin.DisplayName, adminRole.DisplayName);
        }

        [Theory]
        [InlineData("invalid_role")]
        [InlineData("")]
        public void FromValue_WithInvalidValue_ShouldThrowException(string invalidValue)
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Core.Enumeration.Enumeration.FromValue<Role>(invalidValue));
        }

        [Theory]
        [InlineData("Invalid Role")]
        [InlineData("")]
        public void FromDisplayName_WithInvalidName_ShouldThrowException(string invalidName)
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Core.Enumeration.Enumeration.FromDisplayName<Role>(invalidName));
        }

        [Fact]
        public void Contains_ShouldReturnCorrectResult()
        {
            // Assert
            Assert.True(Core.Enumeration.Enumeration.Contains<Role>(Role.OperationValue));
            Assert.True(Core.Enumeration.Enumeration.Contains<Role>(Role.AdminValue));
            Assert.False(Core.Enumeration.Enumeration.Contains<Role>("invalid_role"));
        }
    }
}
