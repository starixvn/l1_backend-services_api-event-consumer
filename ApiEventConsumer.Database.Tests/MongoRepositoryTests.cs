using MongoDB.Driver;
using MongoDB.Bson;
using NSubstitute;

namespace ApiEventConsumer.Database.Tests
{
    public class TestMongoDocument : Document
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    public class MongoRepositoryTests
    {
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<GenericTestDocument> _collection;
        private readonly MongoRepository<GenericTestDocument> _repository;
        private readonly GenericTestDocument _testDocument;
        private readonly List<GenericTestDocument> _testDocuments;

        public MongoRepositoryTests()
        {
            _database = Substitute.For<IMongoDatabase>();
            _collection = Substitute.For<IMongoCollection<GenericTestDocument>>();
            
            // Setup mock collection
            _database.GetCollection<GenericTestDocument>(Arg.Any<string>(), null)
                .Returns(_collection);

            _repository = new MongoRepository<GenericTestDocument>(_database);

            _testDocument = new GenericTestDocument
            {
                Id = ObjectId.GenerateNewId(),
                Name = "Test Document",
                CreatedAt = DateTime.UtcNow
            };

            _testDocuments = new List<GenericTestDocument>
            {
                new GenericTestDocument { Id = ObjectId.GenerateNewId(), Name = "Doc 1" },
                new GenericTestDocument { Id = ObjectId.GenerateNewId(), Name = "Doc 2" }
            };
        }

        [Fact]
        public async Task InsertOneAsync_ShouldCallInsertOne()
        {
            // Act
            await _repository.InsertOneAsync(_testDocument);

            // Assert
            await _collection.Received(1).InsertOneAsync(
                Arg.Is<GenericTestDocument>(d => d.Id == _testDocument.Id),
                Arg.Any<InsertOneOptions>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task FindOneAsync_ShouldReturnDocument()
        {
            // Arrange
            var filter = Builders<GenericTestDocument>.Filter.Eq(x => x.Name, "Test Document");
            var cursor = Substitute.For<IAsyncCursor<GenericTestDocument>>();
            cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(true);
            cursor.Current.Returns(new[] { _testDocument });

            _collection.FindAsync(
                Arg.Any<FilterDefinition<GenericTestDocument>>(),
                Arg.Any<FindOptions<GenericTestDocument>>(),
                Arg.Any<CancellationToken>())
                .Returns(cursor);

            // Act
            var result = await _repository.FindOneAsync(x => x.Name == "Test Document");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(_testDocument.Id, result.Id);
        }

        [Fact]
        public async Task DeleteOneAsync_ShouldCallDeleteOne()
        {
            // Arrange
            var filter = Builders<GenericTestDocument>.Filter.Eq(x => x.Name, "Test Document");
            _collection.FindOneAndDeleteAsync(
                Arg.Any<FilterDefinition<GenericTestDocument>>(),
                Arg.Any<FindOneAndDeleteOptions<GenericTestDocument>>(),
                Arg.Any<CancellationToken>())
                .Returns(_testDocument);

            // Act
            await _repository.DeleteOneAsync(x => x.Name == "Test Document");

            // Assert
            await _collection.Received(1).FindOneAndDeleteAsync(
                Arg.Any<FilterDefinition<GenericTestDocument>>(),
                Arg.Any<FindOneAndDeleteOptions<GenericTestDocument>>(),
                Arg.Any<CancellationToken>());
        }
    }
}
