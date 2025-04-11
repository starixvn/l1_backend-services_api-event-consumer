using ApiEventConsumer.Database.Entities;
using MongoDB.Bson;

namespace ApiEventConsumer.Database.Tests.Account
{
    public class AccountInfoEntityTests
    {
        [Fact]
        public void AccountInfoEntity_ShouldHaveCorrectCollectionName()
        {
            // Act
            var collectionName = Document.GetCollectionName(typeof(AccountInfoEntity));

            // Assert
            Assert.Equal("userinfos", collectionName);
        }

        [Fact]
        public void AccountInfoEntity_Properties_ShouldSetAndGetCorrectly()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var accountInfo = new AccountInfoEntity
            {
                Id = ObjectId.GenerateNewId(),
                UserId = "user123",
                Avatar = "avatar.jpg",
                JoinedOn = now,
                Red = 1000.0,
                RedWin = 500.0,
                RedLost = 200.0,
                RedPlay = 700.0,
                IsBot = false,
                OtpFirst = true,
                AvatarId = 5,
                Currency = "USD",
                Name = "Test User",
                UID = 12345,
                Disabled = false,
                InRoom = true,
                InGameRoom = "room1",
                Spins = 3,
                Banned = false,
                PatternId = 1,
                CreatedAt = now,
                UpdatedAt = now.AddHours(1)
            };

            // Assert
            Assert.NotEqual(ObjectId.Empty, accountInfo.Id);
            Assert.Equal("user123", accountInfo.UserId);
            Assert.Equal("avatar.jpg", accountInfo.Avatar);
            Assert.Equal(now, accountInfo.JoinedOn);
            Assert.Equal(1000.0, accountInfo.Red);
            Assert.Equal(500.0, accountInfo.RedWin);
            Assert.Equal(200.0, accountInfo.RedLost);
            Assert.Equal(700.0, accountInfo.RedPlay);
            Assert.False(accountInfo.IsBot);
            Assert.True(accountInfo.OtpFirst);
            Assert.Equal(5, accountInfo.AvatarId);
            Assert.Equal("USD", accountInfo.Currency);
            Assert.Equal("Test User", accountInfo.Name);
            Assert.Equal(12345, accountInfo.UID);
            Assert.False(accountInfo.Disabled);
            Assert.True(accountInfo.InRoom);
            Assert.Equal("room1", accountInfo.InGameRoom);
            Assert.Equal(3, accountInfo.Spins);
            Assert.False(accountInfo.Banned);
            Assert.Equal(1, accountInfo.PatternId);
        }

        [Fact]
        public void AccountInfoEntity_DefaultValues_ShouldBeCorrect()
        {
            // Arrange
            var accountInfo = new AccountInfoEntity();

            // Assert
            Assert.Equal(ObjectId.Empty, accountInfo.Id);
            Assert.Null(accountInfo.UserId);
            Assert.Null(accountInfo.Avatar);
            Assert.Equal(default(DateTime), accountInfo.JoinedOn);
            Assert.Equal(0.0, accountInfo.Red);
            Assert.Equal(0.0, accountInfo.RedWin);
            Assert.Equal(0.0, accountInfo.RedLost);
            Assert.Equal(0.0, accountInfo.RedPlay);
            Assert.False(accountInfo.IsBot);
            Assert.False(accountInfo.OtpFirst);
            Assert.Equal(0, accountInfo.AvatarId);
            Assert.Null(accountInfo.Currency);
            Assert.Null(accountInfo.Name);
            Assert.Equal(0, accountInfo.UID);
            Assert.Null(accountInfo.Security);
            Assert.False(accountInfo.Disabled);
            Assert.False(accountInfo.InRoom);
            Assert.Null(accountInfo.InGameRoom);
            Assert.Equal(0, accountInfo.Spins);
            Assert.False(accountInfo.Banned);
            Assert.Equal(0, accountInfo.PatternId);
        }

        [Fact]
        public void Security_Properties_ShouldSetAndGetCorrectly()
        {
            // Arrange
            var security = new Security
            {
                Login = 1
            };

            // Assert
            Assert.Equal(1, security.Login);
        }

        [Fact]
        public void Security_DefaultValues_ShouldBeCorrect()
        {
            // Arrange
            var security = new Security();

            // Assert
            Assert.Equal(0, security.Login);
        }

        [Fact]
        public void AccountInfoEntity_WithSecurity_ShouldSetAndGetCorrectly()
        {
            // Arrange
            var accountInfo = new AccountInfoEntity
            {
                UserId = "user123",
                Security = new Security { Login = 1 }
            };

            // Assert
            Assert.NotNull(accountInfo.Security);
            Assert.Equal(1, accountInfo.Security.Login);
        }

        [Theory]
        [InlineData(0.0, 0.0, 0.0)]
        [InlineData(1000.0, 500.0, 200.0)]
        [InlineData(-100.0, -50.0, -20.0)]
        public void AccountInfoEntity_ShouldHandleVariousRedValues(double red, double redWin, double redLost)
        {
            // Arrange
            var accountInfo = new AccountInfoEntity
            {
                Red = red,
                RedWin = redWin,
                RedLost = redLost
            };

            // Assert
            Assert.Equal(red, accountInfo.Red);
            Assert.Equal(redWin, accountInfo.RedWin);
            Assert.Equal(redLost, accountInfo.RedLost);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public void AccountInfoEntity_ShouldHandleVariousStates(bool inRoom, bool banned)
        {
            // Arrange
            var accountInfo = new AccountInfoEntity
            {
                InRoom = inRoom,
                Banned = banned
            };

            // Assert
            Assert.Equal(inRoom, accountInfo.InRoom);
            Assert.Equal(banned, accountInfo.Banned);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(255)]
        public void AccountInfoEntity_ShouldHandleValidSpinValues(byte spins)
        {
            // Arrange
            var accountInfo = new AccountInfoEntity
            {
                Spins = spins
            };

            // Assert
            Assert.Equal(spins, accountInfo.Spins);
        }

        [Fact]
        public void AccountInfoEntity_RedPlayShouldReflectActivity()
        {
            // Arrange
            var accountInfo = new AccountInfoEntity
            {
                RedPlay = 1000.0,
                RedWin = 600.0,
                RedLost = 400.0
            };

            // Assert
            Assert.Equal(accountInfo.RedWin + accountInfo.RedLost, accountInfo.RedPlay);
        }
    }
}
