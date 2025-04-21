using MongoDB.Driver;
using MongoDB.Bson;
using Moq;

namespace ApiEventConsumer.Database.Tests
{
    public class TestMongoDocument : Document
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    public class MongoRepositoryTests
    {
        private readonly Mock<IMongoDatabase> _mockDatabase;
        private readonly Mock<IMongoCollection<GenericTestDocument>> _mockCollection;
        private readonly MongoRepository<GenericTestDocument> _repository;
        private readonly GenericTestDocument _testDocument;
        private readonly List<GenericTestDocument> _testDocuments;

        public MongoRepositoryTests()
        {
            _mockDatabase = new Mock<IMongoDatabase>();
            _mockCollection = new Mock<IMongoCollection<GenericTestDocument>>();
            
            // Setup mock collection
            _mockDatabase.Setup(db => db.GetCollection<GenericTestDocument>(It.IsAny<string>(), null))
                .Returns(_mockCollection.Object);

            _repository = new MongoRepository<GenericTestDocument>(_mockDatabase.Object);

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
            // Arrange
            _mockCollection.Setup(c => c.InsertOneAsync(
                It.IsAny<GenericTestDocument>(),
                It.IsAny<InsertOneOptions>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _repository.InsertOneAsync(_testDocument);

            // Assert
            _mockCollection.Verify(c => c.InsertOneAsync(
                It.Is<GenericTestDocument>(d => d.Id == _testDocument.Id),
                It.IsAny<InsertOneOptions>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task FindOneAsync_ShouldReturnDocument()
        {
            // Arrange
            var filter = Builders<GenericTestDocument>.Filter.Eq(x => x.Name, "Test Document");
            var mockCursor = new Mock<IAsyncCursor<GenericTestDocument>>();
            mockCursor.Setup(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            mockCursor.Setup(c => c.Current)
                .Returns(new[] { _testDocument });

            _mockCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<GenericTestDocument>>(),
                It.IsAny<FindOptions<GenericTestDocument>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);

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
            _mockCollection.Setup(c => c.FindOneAndDeleteAsync(
                It.IsAny<FilterDefinition<GenericTestDocument>>(),
                It.IsAny<FindOneAndDeleteOptions<GenericTestDocument>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(_testDocument);

            // Act
            await _repository.DeleteOneAsync(x => x.Name == "Test Document");

            // Assert
            _mockCollection.Verify(c => c.FindOneAndDeleteAsync(
                It.IsAny<FilterDefinition<GenericTestDocument>>(),
                It.IsAny<FindOneAndDeleteOptions<GenericTestDocument>>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
