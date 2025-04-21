using ApiEventConsumer.Database.Entities;
using MongoDB.Bson;

namespace ApiEventConsumer.Database.Tests.Account
{
    public class AccountEntityTests
    {
        [Fact]
        public void AccountEntity_ShouldHaveCorrectCollectionName()
        {
            // Act
            var collectionName = Document.GetCollectionName(typeof(AccountEntity));

            // Assert
            Assert.Equal("users", collectionName);
        }

        [Fact]
        public void AccountEntity_Properties_ShouldSetAndGetCorrectly()
        {
            // Arrange
            var account = new AccountEntity
            {
                Id = ObjectId.GenerateNewId(),
                UserIdStr = "user123",
                OperatorId = "op456",
                PlayerId = "player789",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow.AddHours(1)
            };

            // Assert
            Assert.NotEqual(ObjectId.Empty, account.Id);
            Assert.Equal("user123", account.UserIdStr);
            Assert.Equal("op456", account.OperatorId);
            Assert.Equal("player789", account.PlayerId);
            Assert.NotEqual(default(DateTime), account.CreatedAt);
            Assert.NotNull(account.UpdatedAt);
        }

        [Fact]
        public void AccountEntity_DefaultValues_ShouldBeCorrect()
        {
            // Arrange
            var account = new AccountEntity();

            // Assert
            Assert.Equal(ObjectId.Empty, account.Id);
            Assert.Null(account.Local);
            Assert.Null(account.UserIdStr);
            Assert.Null(account.OperatorId);
            Assert.Null(account.PlayerId);
            Assert.Equal(default(DateTime), account.CreatedAt);
            Assert.Null(account.UpdatedAt);
        }

        [Fact]
        public void LocalAccount_Properties_ShouldSetAndGetCorrectly()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var localAccount = new LocalAccount
            {
                Ban_pass = 3,
                Ban_login = true,
                Username = "testuser",
                Password = "hashedpassword",
                regDate = now,
                MemberId = "member123",
                MemberName = "Test Member",
                Language = "en"
            };

            // Assert
            Assert.Equal(3, localAccount.Ban_pass);
            Assert.True(localAccount.Ban_login);
            Assert.Equal("testuser", localAccount.Username);
            Assert.Equal("hashedpassword", localAccount.Password);
            Assert.Equal(now, localAccount.regDate);
            Assert.Equal("member123", localAccount.MemberId);
            Assert.Equal("Test Member", localAccount.MemberName);
            Assert.Equal("en", localAccount.Language);
        }

        [Fact]
        public void LocalAccount_DefaultValues_ShouldBeCorrect()
        {
            // Arrange
            var localAccount = new LocalAccount();

            // Assert
            Assert.Equal(0, localAccount.Ban_pass);
            Assert.False(localAccount.Ban_login);
            Assert.Null(localAccount.Username);
            Assert.Null(localAccount.Password);
            Assert.Null(localAccount.regDate);
            Assert.Null(localAccount.MemberId);
            Assert.Null(localAccount.MemberName);
            Assert.Null(localAccount.Language);
        }

        [Fact]
        public void AccountEntity_WithLocalAccount_ShouldSetAndGetCorrectly()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var account = new AccountEntity
            {
                UserIdStr = "user123",
                Local = new LocalAccount
                {
                    Username = "testuser",
                    Password = "hashedpassword",
                    Ban_pass = 1,
                    Ban_login = true,
                    regDate = now,
                    MemberId = "member123",
                    MemberName = "Test Member",
                    Language = "en"
                }
            };

            // Assert
            Assert.NotNull(account.Local);
            Assert.Equal("testuser", account.Local.Username);
            Assert.Equal("hashedpassword", account.Local.Password);
            Assert.Equal(1, account.Local.Ban_pass);
            Assert.True(account.Local.Ban_login);
            Assert.Equal(now, account.Local.regDate);
            Assert.Equal("member123", account.Local.MemberId);
            Assert.Equal("Test Member", account.Local.MemberName);
            Assert.Equal("en", account.Local.Language);
        }

        [Theory]
        [InlineData("en")]
        [InlineData("vi")]
        [InlineData(null)]
        public void LocalAccount_ShouldHandleVariousLanguages(string? language)
        {
            // Arrange
            var localAccount = new LocalAccount { Language = language };

            // Assert
            Assert.Equal(language, localAccount.Language);
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(1, true)]
        [InlineData(3, true)]
        public void LocalAccount_ShouldHandleVariousBanStates(int banPass, bool banLogin)
        {
            // Arrange
            var localAccount = new LocalAccount
            {
                Ban_pass = banPass,
                Ban_login = banLogin
            };

            // Assert
            Assert.Equal(banPass, localAccount.Ban_pass);
            Assert.Equal(banLogin, localAccount.Ban_login);
        }
    }
}
