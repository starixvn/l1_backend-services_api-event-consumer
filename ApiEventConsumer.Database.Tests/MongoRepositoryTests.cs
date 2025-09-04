using MongoDB.Driver;
using MongoDB.Bson;
using NSubstitute;
using System.Linq.Expressions;

namespace ApiEventConsumer.Database.Tests
{
    public class TestMongoDocument : Document
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    public class MongoRepositoryTests
    {
        [Fact]
        public void Constructor_WithValidDatabase_ShouldCreateRepository()
        {
            // Arrange
            var database = Substitute.For<IMongoDatabase>();
            var collection = Substitute.For<IMongoCollection<GenericTestDocument>>();
            database.GetCollection<GenericTestDocument>("generictestdocument", null).Returns(collection);

            // Act & Assert
            var exception = Record.Exception(() => new MongoRepository<GenericTestDocument>(database));
            Assert.Null(exception);
        }

        [Fact]
        public void Constructor_ShouldCallGetCollectionWithCorrectName()
        {
            // Arrange
            var database = Substitute.For<IMongoDatabase>();
            var collection = Substitute.For<IMongoCollection<GenericTestDocument>>();

            // Act
            var repository = new MongoRepository<GenericTestDocument>(database);

            // Assert
            database.Received().GetCollection<GenericTestDocument>(Arg.Any<string>(), null);
        }

        [Fact]
        public void Collection_ShouldReturnCorrectCollection()
        {
            // Arrange
            var database = Substitute.For<IMongoDatabase>();
            var collection = Substitute.For<IMongoCollection<GenericTestDocument>>();
            database.GetCollection<GenericTestDocument>(Arg.Any<string>(), null).Returns(collection);

            var repository = new MongoRepository<GenericTestDocument>(database);

            // Act
            var result = repository.Collection();

            // Assert
            Assert.NotNull(result);
            Assert.Same(collection, result);
            Assert.IsAssignableFrom<IMongoCollection<GenericTestDocument>>(result);
        }

        // Basic existence tests - ensure all methods exist and can be called
        [Fact]
        public void AsQueryable_ShouldExistAndBeCallable()
        {
            // Arrange
            var database = Substitute.For<IMongoDatabase>();
            var collection = Substitute.For<IMongoCollection<GenericTestDocument>>();
            database.GetCollection<GenericTestDocument>(Arg.Any<string>(), null).Returns(collection);

            var repository = new MongoRepository<GenericTestDocument>(database);

            // Act & Assert - Just ensure method exists
            var methodInfo = typeof(MongoRepository<GenericTestDocument>).GetMethod("AsQueryable");
            Assert.NotNull(methodInfo);
            Assert.Equal(typeof(IQueryable<GenericTestDocument>), methodInfo.ReturnType);
        }

        [Fact]
        public void FilterBy_ShouldExistWithCorrectSignature()
        {
            // Arrange & Act
            var database = Substitute.For<IMongoDatabase>();
            var collection = Substitute.For<IMongoCollection<GenericTestDocument>>();
            database.GetCollection<GenericTestDocument>(Arg.Any<string>(), null).Returns(collection);

            var repository = new MongoRepository<GenericTestDocument>(database);

            // Assert - Verify method signatures exist
            var filterByMethods = typeof(MongoRepository<GenericTestDocument>).GetMethods().Where(m => m.Name == "FilterBy");
            Assert.Equal(2, filterByMethods.Count()); // Should have 2 overloads
        }

        [Fact]
        public void FilterByAsync_ShouldExistWithCorrectSignature()
        {
            // Arrange
            var database = Substitute.For<IMongoDatabase>();
            var collection = Substitute.For<IMongoCollection<GenericTestDocument>>();
            database.GetCollection<GenericTestDocument>(Arg.Any<string>(), null).Returns(collection);

            var repository = new MongoRepository<GenericTestDocument>(database);

            // Act & Assert
            var methodInfo = typeof(MongoRepository<GenericTestDocument>).GetMethod("FilterByAsync");
            Assert.NotNull(methodInfo);
            Assert.True(methodInfo.ReturnType.IsGenericType);
            Assert.Equal(typeof(Task<>), methodInfo.ReturnType.GetGenericTypeDefinition());
        }

        [Fact]
        public void FindOne_ShouldExistWithCorrectSignature()
        {
            // Arrange
            var database = Substitute.For<IMongoDatabase>();
            var collection = Substitute.For<IMongoCollection<GenericTestDocument>>();
            database.GetCollection<GenericTestDocument>(Arg.Any<string>(), null).Returns(collection);

            var repository = new MongoRepository<GenericTestDocument>(database);

            // Act & Assert
            var methodInfo = typeof(MongoRepository<GenericTestDocument>).GetMethod("FindOne");
            Assert.NotNull(methodInfo);
            Assert.Equal(typeof(GenericTestDocument), methodInfo.ReturnType);
        }

        [Fact]
        public void FindOneAsync_ShouldExistWithCorrectSignature()
        {
            // Arrange
            var database = Substitute.For<IMongoDatabase>();
            var collection = Substitute.For<IMongoCollection<GenericTestDocument>>();
            database.GetCollection<GenericTestDocument>(Arg.Any<string>(), null).Returns(collection);

            var repository = new MongoRepository<GenericTestDocument>(database);

            // Act & Assert
            var methodInfo = typeof(MongoRepository<GenericTestDocument>).GetMethod("FindOneAsync");
            Assert.NotNull(methodInfo);
            Assert.True(methodInfo.ReturnType.IsGenericType);
            Assert.Equal(typeof(Task<>), methodInfo.ReturnType.GetGenericTypeDefinition());
        }

        [Fact]
        public void FindById_ShouldExistWithCorrectSignature()
        {
            // Arrange
            var database = Substitute.For<IMongoDatabase>();
            var collection = Substitute.For<IMongoCollection<GenericTestDocument>>();
            database.GetCollection<GenericTestDocument>(Arg.Any<string>(), null).Returns(collection);

            var repository = new MongoRepository<GenericTestDocument>(database);

            // Act & Assert
            var methodInfo = typeof(MongoRepository<GenericTestDocument>).GetMethod("FindById");
            Assert.NotNull(methodInfo);
            Assert.Equal(typeof(GenericTestDocument), methodInfo.ReturnType);
            
            var parameters = methodInfo.GetParameters();
            Assert.Single(parameters);
            Assert.Equal(typeof(string), parameters[0].ParameterType);
        }

        [Fact]
        public void FindByIdAsync_ShouldExistWithCorrectSignature()
        {
            // Arrange
            var database = Substitute.For<IMongoDatabase>();
            var collection = Substitute.For<IMongoCollection<GenericTestDocument>>();
            database.GetCollection<GenericTestDocument>(Arg.Any<string>(), null).Returns(collection);

            var repository = new MongoRepository<GenericTestDocument>(database);

            // Act & Assert
            var methodInfo = typeof(MongoRepository<GenericTestDocument>).GetMethod("FindByIdAsync");
            Assert.NotNull(methodInfo);
            Assert.True(methodInfo.ReturnType.IsGenericType);
            Assert.Equal(typeof(Task<>), methodInfo.ReturnType.GetGenericTypeDefinition());

            var parameters = methodInfo.GetParameters();
            Assert.Single(parameters);
            Assert.Equal(typeof(string), parameters[0].ParameterType);
        }

        [Fact]
        public void InsertOne_ShouldExistWithCorrectSignature()
        {
            // Arrange
            var database = Substitute.For<IMongoDatabase>();
            var collection = Substitute.For<IMongoCollection<GenericTestDocument>>();
            database.GetCollection<GenericTestDocument>(Arg.Any<string>(), null).Returns(collection);

            var repository = new MongoRepository<GenericTestDocument>(database);

            // Act & Assert
            var methodInfo = typeof(MongoRepository<GenericTestDocument>).GetMethod("InsertOne");
            Assert.NotNull(methodInfo);
            Assert.Equal(typeof(void), methodInfo.ReturnType);
            
            var parameters = methodInfo.GetParameters();
            Assert.Single(parameters);
            Assert.Equal(typeof(GenericTestDocument), parameters[0].ParameterType);
        }

        [Fact]
        public void InsertOneAsync_ShouldExistWithCorrectSignature()
        {
            // Arrange
            var database = Substitute.For<IMongoDatabase>();
            var collection = Substitute.For<IMongoCollection<GenericTestDocument>>();
            database.GetCollection<GenericTestDocument>(Arg.Any<string>(), null).Returns(collection);

            var repository = new MongoRepository<GenericTestDocument>(database);

            // Act & Assert
            var methodInfo = typeof(MongoRepository<GenericTestDocument>).GetMethod("InsertOneAsync");
            Assert.NotNull(methodInfo);
            Assert.Equal(typeof(Task), methodInfo.ReturnType);
            
            var parameters = methodInfo.GetParameters();
            Assert.Single(parameters);
            Assert.Equal(typeof(GenericTestDocument), parameters[0].ParameterType);
        }

        [Fact]
        public void InsertMany_ShouldExistWithCorrectSignature()
        {
            // Arrange
            var database = Substitute.For<IMongoDatabase>();
            var collection = Substitute.For<IMongoCollection<GenericTestDocument>>();
            database.GetCollection<GenericTestDocument>(Arg.Any<string>(), null).Returns(collection);

            var repository = new MongoRepository<GenericTestDocument>(database);

            // Act & Assert
            var methodInfo = typeof(MongoRepository<GenericTestDocument>).GetMethod("InsertMany");
            Assert.NotNull(methodInfo);
            Assert.Equal(typeof(void), methodInfo.ReturnType);
            
            var parameters = methodInfo.GetParameters();
            Assert.Single(parameters);
            Assert.True(parameters[0].ParameterType.IsAssignableFrom(typeof(IEnumerable<GenericTestDocument>)));
        }

        [Fact]
        public void InsertManyAsync_ShouldExistWithCorrectSignature()
        {
            // Arrange
            var database = Substitute.For<IMongoDatabase>();
            var collection = Substitute.For<IMongoCollection<GenericTestDocument>>();
            database.GetCollection<GenericTestDocument>(Arg.Any<string>(), null).Returns(collection);

            var repository = new MongoRepository<GenericTestDocument>(database);

            // Act & Assert
            var methodInfo = typeof(MongoRepository<GenericTestDocument>).GetMethod("InsertManyAsync");
            Assert.NotNull(methodInfo);
            Assert.Equal(typeof(Task), methodInfo.ReturnType);
            
            var parameters = methodInfo.GetParameters();
            Assert.Single(parameters);
            Assert.True(parameters[0].ParameterType.IsAssignableFrom(typeof(IEnumerable<GenericTestDocument>)));
        }

        [Fact]
        public void ReplaceOne_ShouldExistWithCorrectSignature()
        {
            // Arrange
            var database = Substitute.For<IMongoDatabase>();
            var collection = Substitute.For<IMongoCollection<GenericTestDocument>>();
            database.GetCollection<GenericTestDocument>(Arg.Any<string>(), null).Returns(collection);

            var repository = new MongoRepository<GenericTestDocument>(database);

            // Act & Assert
            var methodInfo = typeof(MongoRepository<GenericTestDocument>).GetMethod("ReplaceOne");
            Assert.NotNull(methodInfo);
            Assert.Equal(typeof(void), methodInfo.ReturnType);
            
            var parameters = methodInfo.GetParameters();
            Assert.Single(parameters);
            Assert.Equal(typeof(GenericTestDocument), parameters[0].ParameterType);
        }

        [Fact]
        public void ReplaceOneAsync_ShouldExistWithCorrectSignature()
        {
            // Arrange
            var database = Substitute.For<IMongoDatabase>();
            var collection = Substitute.For<IMongoCollection<GenericTestDocument>>();
            database.GetCollection<GenericTestDocument>(Arg.Any<string>(), null).Returns(collection);

            var repository = new MongoRepository<GenericTestDocument>(database);

            // Act & Assert
            var methodInfo = typeof(MongoRepository<GenericTestDocument>).GetMethod("ReplaceOneAsync");
            Assert.NotNull(methodInfo);
            Assert.Equal(typeof(Task), methodInfo.ReturnType);
            
            var parameters = methodInfo.GetParameters();
            Assert.Single(parameters);
            Assert.Equal(typeof(GenericTestDocument), parameters[0].ParameterType);
        }

        [Fact]
        public void DeleteOne_ShouldExistWithCorrectSignature()
        {
            // Arrange
            var database = Substitute.For<IMongoDatabase>();
            var collection = Substitute.For<IMongoCollection<GenericTestDocument>>();
            database.GetCollection<GenericTestDocument>(Arg.Any<string>(), null).Returns(collection);

            var repository = new MongoRepository<GenericTestDocument>(database);

            // Act & Assert
            var methodInfo = typeof(MongoRepository<GenericTestDocument>).GetMethod("DeleteOne");
            Assert.NotNull(methodInfo);
            Assert.Equal(typeof(void), methodInfo.ReturnType);
            
            var parameters = methodInfo.GetParameters();
            Assert.Single(parameters);
            Assert.True(parameters[0].ParameterType.IsGenericType);
        }

        [Fact]
        public void DeleteOneAsync_ShouldExistWithCorrectSignature()
        {
            // Arrange
            var database = Substitute.For<IMongoDatabase>();
            var collection = Substitute.For<IMongoCollection<GenericTestDocument>>();
            database.GetCollection<GenericTestDocument>(Arg.Any<string>(), null).Returns(collection);

            var repository = new MongoRepository<GenericTestDocument>(database);

            // Act & Assert
            var methodInfo = typeof(MongoRepository<GenericTestDocument>).GetMethod("DeleteOneAsync");
            Assert.NotNull(methodInfo);
            Assert.Equal(typeof(Task), methodInfo.ReturnType);
            
            var parameters = methodInfo.GetParameters();
            Assert.Single(parameters);
            Assert.True(parameters[0].ParameterType.IsGenericType);
        }

        [Fact]
        public void DeleteById_ShouldExistWithCorrectSignature()
        {
            // Arrange
            var database = Substitute.For<IMongoDatabase>();
            var collection = Substitute.For<IMongoCollection<GenericTestDocument>>();
            database.GetCollection<GenericTestDocument>(Arg.Any<string>(), null).Returns(collection);

            var repository = new MongoRepository<GenericTestDocument>(database);

            // Act & Assert
            var methodInfo = typeof(MongoRepository<GenericTestDocument>).GetMethod("DeleteById");
            Assert.NotNull(methodInfo);
            Assert.Equal(typeof(void), methodInfo.ReturnType);
            
            var parameters = methodInfo.GetParameters();
            Assert.Single(parameters);
            Assert.Equal(typeof(string), parameters[0].ParameterType);
        }

        [Fact]
        public void DeleteByIdAsync_ShouldExistWithCorrectSignature()
        {
            // Arrange
            var database = Substitute.For<IMongoDatabase>();
            var collection = Substitute.For<IMongoCollection<GenericTestDocument>>();
            database.GetCollection<GenericTestDocument>(Arg.Any<string>(), null).Returns(collection);

            var repository = new MongoRepository<GenericTestDocument>(database);

            // Act & Assert
            var methodInfo = typeof(MongoRepository<GenericTestDocument>).GetMethod("DeleteByIdAsync");
            Assert.NotNull(methodInfo);
            Assert.Equal(typeof(Task), methodInfo.ReturnType);
            
            var parameters = methodInfo.GetParameters();
            Assert.Single(parameters);
            Assert.Equal(typeof(string), parameters[0].ParameterType);
        }

        [Fact]
        public void DeleteMany_ShouldExistWithCorrectSignature()
        {
            // Arrange
            var database = Substitute.For<IMongoDatabase>();
            var collection = Substitute.For<IMongoCollection<GenericTestDocument>>();
            database.GetCollection<GenericTestDocument>(Arg.Any<string>(), null).Returns(collection);

            var repository = new MongoRepository<GenericTestDocument>(database);

            // Act & Assert
            var methodInfo = typeof(MongoRepository<GenericTestDocument>).GetMethod("DeleteMany");
            Assert.NotNull(methodInfo);
            Assert.Equal(typeof(void), methodInfo.ReturnType);
            
            var parameters = methodInfo.GetParameters();
            Assert.Single(parameters);
            Assert.True(parameters[0].ParameterType.IsGenericType);
        }

        [Fact]
        public void DeleteManyAsync_ShouldExistWithCorrectSignature()
        {
            // Arrange
            var database = Substitute.For<IMongoDatabase>();
            var collection = Substitute.For<IMongoCollection<GenericTestDocument>>();
            database.GetCollection<GenericTestDocument>(Arg.Any<string>(), null).Returns(collection);

            var repository = new MongoRepository<GenericTestDocument>(database);

            // Act & Assert
            var methodInfo = typeof(MongoRepository<GenericTestDocument>).GetMethod("DeleteManyAsync");
            Assert.NotNull(methodInfo);
            Assert.Equal(typeof(Task), methodInfo.ReturnType);
            
            var parameters = methodInfo.GetParameters();
            Assert.Single(parameters);
            Assert.True(parameters[0].ParameterType.IsGenericType);
        }

        [Fact]
        public void Update_ShouldExistWithCorrectSignature()
        {
            // Arrange
            var database = Substitute.For<IMongoDatabase>();
            var collection = Substitute.For<IMongoCollection<GenericTestDocument>>();
            database.GetCollection<GenericTestDocument>(Arg.Any<string>(), null).Returns(collection);

            var repository = new MongoRepository<GenericTestDocument>(database);

            // Act & Assert
            var methodInfo = typeof(MongoRepository<GenericTestDocument>).GetMethod("Update");
            Assert.NotNull(methodInfo);
            Assert.Equal(typeof(void), methodInfo.ReturnType);
            
            var parameters = methodInfo.GetParameters();
            Assert.Equal(3, parameters.Length); // filter, updateDefinition, options
        }

        [Fact]
        public void UpdateAsync_ShouldExistWithCorrectSignature()
        {
            // Arrange
            var database = Substitute.For<IMongoDatabase>();
            var collection = Substitute.For<IMongoCollection<GenericTestDocument>>();
            database.GetCollection<GenericTestDocument>(Arg.Any<string>(), null).Returns(collection);

            var repository = new MongoRepository<GenericTestDocument>(database);

            // Act & Assert
            var methodInfo = typeof(MongoRepository<GenericTestDocument>).GetMethod("UpdateAsync");
            Assert.NotNull(methodInfo);
            Assert.Equal(typeof(Task), methodInfo.ReturnType);
            
            var parameters = methodInfo.GetParameters();
            Assert.Equal(3, parameters.Length); // filter, updateDefinition, options
        }

        [Fact]
        public void UpdateManyAsync_ShouldExistWithCorrectSignature()
        {
            // Arrange
            var database = Substitute.For<IMongoDatabase>();
            var collection = Substitute.For<IMongoCollection<GenericTestDocument>>();
            database.GetCollection<GenericTestDocument>(Arg.Any<string>(), null).Returns(collection);

            var repository = new MongoRepository<GenericTestDocument>(database);

            // Act & Assert
            var methodInfo = typeof(MongoRepository<GenericTestDocument>).GetMethod("UpdateManyAsync");
            Assert.NotNull(methodInfo);
            Assert.Equal(typeof(Task), methodInfo.ReturnType);
            
            var parameters = methodInfo.GetParameters();
            Assert.Equal(3, parameters.Length); // filter, updateDefinition, options
        }

        [Fact]
        public void Aggregate_ShouldExistWithCorrectSignature()
        {
            // Arrange
            var database = Substitute.For<IMongoDatabase>();
            var collection = Substitute.For<IMongoCollection<GenericTestDocument>>();
            database.GetCollection<GenericTestDocument>(Arg.Any<string>(), null).Returns(collection);

            var repository = new MongoRepository<GenericTestDocument>(database);

            // Act & Assert
            var methodInfo = typeof(MongoRepository<GenericTestDocument>).GetMethod("Aggregate");
            Assert.NotNull(methodInfo);
            Assert.True(methodInfo.ReturnType.IsGenericType);
            Assert.Equal(typeof(IAggregateFluent<>), methodInfo.ReturnType.GetGenericTypeDefinition());
        }

        [Fact]
        public void RepositoryImplementsCorrectInterface()
        {
            // Arrange
            var database = Substitute.For<IMongoDatabase>();
            var collection = Substitute.For<IMongoCollection<GenericTestDocument>>();
            database.GetCollection<GenericTestDocument>(Arg.Any<string>(), null).Returns(collection);

            var repository = new MongoRepository<GenericTestDocument>(database);

            // Act & Assert
            Assert.IsAssignableFrom<IGenericRepository<GenericTestDocument>>(repository);
        }

        [Fact]
        public void AllRequiredMethods_ShouldBeImplemented()
        {
            // Arrange
            var database = Substitute.For<IMongoDatabase>();
            var collection = Substitute.For<IMongoCollection<GenericTestDocument>>();
            database.GetCollection<GenericTestDocument>(Arg.Any<string>(), null).Returns(collection);

            var repository = new MongoRepository<GenericTestDocument>(database);

            // Act & Assert - Verify all required methods exist
            var expectedMethods = new[]
            {
                "AsQueryable", "FilterBy", "FilterByAsync", "FindOne", "FindOneAsync", 
                "FindById", "FindByIdAsync", "InsertOne", "InsertOneAsync", "InsertMany", 
                "InsertManyAsync", "ReplaceOne", "ReplaceOneAsync", "DeleteOne", "DeleteOneAsync",
                "DeleteById", "DeleteByIdAsync", "DeleteMany", "DeleteManyAsync", 
                "Update", "UpdateAsync", "UpdateManyAsync", "Aggregate", "Collection"
            };

            var actualMethods = typeof(MongoRepository<GenericTestDocument>).GetMethods()
                .Where(m => m.IsPublic && !m.IsSpecialName && expectedMethods.Contains(m.Name))
                .Select(m => m.Name)
                .Distinct()
                .ToArray();

            foreach (var expectedMethod in expectedMethods)
            {
                Assert.Contains(expectedMethod, actualMethods);
            }
        }
    }
}