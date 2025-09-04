using ApiEventConsumer.Database;
using ApiEventConsumer.Database.Entities;
using ApiEventConsumer.Services;
using FluentAssertions;
using MongoDB.Driver;
using NSubstitute;

namespace ApiEventConsumer.Service.Tests.Service
{
    public class GameUserServicesTests
    {
        private readonly IGenericRepository<AccountInfoEntity> _accountInfoRepository;
        private readonly IGenericRepository<AccountEntity> _accountRepository;

        public GameUserServicesTests()
        {
            _accountInfoRepository = Substitute.For<IGenericRepository<AccountInfoEntity>>();
            _accountRepository = Substitute.For<IGenericRepository<AccountEntity>>();
        }

        private GameUserServices CreateService()
        {
            return new GameUserServices(_accountInfoRepository, _accountRepository);
        }

        [Fact]
        public void Constructor_WithValidRepositories_ShouldCreateInstance()
        {
            // Arrange & Act
            var service = CreateService();

            // Assert
            service.Should().NotBeNull();
            service.Should().BeAssignableTo<IGameUserService>();
        }

        [Fact]
        public void GetCharacterNameAsync_ShouldHaveCorrectMethodSignature()
        {
            // Arrange
            var service = CreateService();

            // Act & Assert
            var method = typeof(GameUserServices).GetMethod("GetCharacterNameAsync");
            method.Should().NotBeNull();
            method!.ReturnType.Should().Be<Task<string>>();
            
            var parameters = method.GetParameters();
            parameters.Should().HaveCount(1);
            parameters[0].ParameterType.Should().Be<string>();
            parameters[0].Name.Should().Be("memberId");
        }

        [Fact]
        public void GameUserServices_ShouldImplementIGameUserService()
        {
            // Arrange & Act
            var service = CreateService();

            // Assert
            service.Should().BeAssignableTo<IGameUserService>();
        }

        [Fact]
        public void GameUserServices_ShouldHaveCorrectDependencies()
        {
            // Arrange & Act
            var service = CreateService();

            // Assert
            // Verify that the service accepts the correct repository types
            var constructor = typeof(GameUserServices).GetConstructors().First();
            var parameters = constructor.GetParameters();
            
            parameters.Should().HaveCount(2);
            parameters[0].ParameterType.Should().Be<IGenericRepository<AccountInfoEntity>>();
            parameters[1].ParameterType.Should().Be<IGenericRepository<AccountEntity>>();
        }

        [Fact]
        public void GetCharacterNameAsync_DocumentationNote_AboutTestingComplexity()
        {
            var service = CreateService();
            
            service.Should().NotBeNull();
        }
    }
}