using MongoDB.Driver;
using MongoDB.Bson;
using Mongo2Go;

namespace ApiEventConsumer.Database.Tests
{
    public class TestMongoDocument : Document
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    public class MongoRepositoryTests : IDisposable
    {
        private readonly MongoDbRunner _runner;
        private readonly IMongoDatabase _database;
        private readonly MongoRepository<TestMongoDocument> _repository;
        private readonly TestMongoDocument _testDocument;
        private readonly List<TestMongoDocument> _testDocuments;

        public MongoRepositoryTests()
        {
            _runner = MongoDbRunner.Start();
            var client = new MongoClient(_runner.ConnectionString);
            _database = client.GetDatabase("test-db");
            _repository = new MongoRepository<TestMongoDocument>(_database);

            _testDocument = new TestMongoDocument
            {
                Id = ObjectId.GenerateNewId(),
                Name = "Test Document",
                Age = 25,
                CreatedAt = DateTime.UtcNow
            };

            _testDocuments = new List<TestMongoDocument>
            {
                new TestMongoDocument { Id = ObjectId.GenerateNewId(), Name = "Doc 1", Age = 20 },
                new TestMongoDocument { Id = ObjectId.GenerateNewId(), Name = "Doc 2", Age = 30 }
            };
        }

        public void Dispose()
        {
            _runner.Dispose();
        }

        [Fact]
        public async Task InsertOneAsync_ShouldInsertDocument()
        {
            // Act
            await _repository.InsertOneAsync(_testDocument);

            // Assert
            var result = await _repository.FindByIdAsync(_testDocument.Id.ToString());
            Assert.NotNull(result);
            Assert.Equal(_testDocument.Name, result.Name);
            Assert.Equal(_testDocument.Age, result.Age);
        }

        [Fact]
        public async Task InsertManyAsync_ShouldInsertMultipleDocuments()
        {
            // Act
            await _repository.InsertManyAsync(_testDocuments);

            // Assert
            var results = _repository.AsQueryable().ToList();
            Assert.Equal(_testDocuments.Count, results.Count);
        }

        [Fact]
        public async Task FindOneAsync_ShouldReturnCorrectDocument()
        {
            // Arrange
            await _repository.InsertOneAsync(_testDocument);

            // Act
            var result = await _repository.FindOneAsync(x => x.Name == _testDocument.Name);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(_testDocument.Name, result.Name);
        }

        [Fact]
        public async Task FilterByAsync_ShouldReturnFilteredDocuments()
        {
            // Arrange
            await _repository.InsertManyAsync(_testDocuments);

            // Act
            var results = await _repository.FilterByAsync(x => x.Age > 25);

            // Assert
            Assert.Single(results);
            Assert.Equal(30, results.First().Age);
        }

        [Fact]
        public async Task UpdateAsync_ShouldModifyDocument()
        {
            // Arrange
            await _repository.InsertOneAsync(_testDocument);
            var update = Builders<TestMongoDocument>.Update.Set(x => x.Name, "Updated Name");

            // Act
            await _repository.UpdateAsync(x => x.Id == _testDocument.Id, update);

            // Assert
            var updated = await _repository.FindByIdAsync(_testDocument.Id.ToString());
            Assert.Equal("Updated Name", updated.Name);
        }

        [Fact]
        public async Task DeleteOneAsync_ShouldRemoveDocument()
        {
            // Arrange
            await _repository.InsertOneAsync(_testDocument);

            // Act
            await _repository.DeleteOneAsync(x => x.Id == _testDocument.Id);

            // Assert
            var result = await _repository.FindByIdAsync(_testDocument.Id.ToString());
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteManyAsync_ShouldRemoveMultipleDocuments()
        {
            // Arrange
            await _repository.InsertManyAsync(_testDocuments);

            // Act
            await _repository.DeleteManyAsync(x => x.Age >= 20);

            // Assert
            var remaining = _repository.AsQueryable().ToList();
            Assert.Empty(remaining);
        }

        [Fact]
        public async Task ReplaceOneAsync_ShouldReplaceDocument()
        {
            // Arrange
            await _repository.InsertOneAsync(_testDocument);
            var replacement = new TestMongoDocument
            {
                Id = _testDocument.Id,
                Name = "Replaced Document",
                Age = 40
            };

            // Act
            await _repository.ReplaceOneAsync(replacement);

            // Assert
            var result = await _repository.FindByIdAsync(_testDocument.Id.ToString());
            Assert.Equal("Replaced Document", result.Name);
            Assert.Equal(40, result.Age);
        }

        [Fact]
        public async Task FindById_ShouldReturnCorrectDocument()
        {
            // Arrange
            await _repository.InsertOneAsync(_testDocument);

            // Act
            var result = _repository.FindById(_testDocument.Id.ToString());

            // Assert
            Assert.NotNull(result);
            Assert.Equal(_testDocument.Name, result.Name);
        }

        [Fact]
        public void Aggregate_ShouldReturnAggregateFluent()
        {
            // Act
            var aggregate = _repository.Aggregate();

            // Assert
            Assert.NotNull(aggregate);
            Assert.IsAssignableFrom<IAggregateFluent<TestMongoDocument>>(aggregate);
        }

        [Fact]
        public async Task FilterBy_WithProjection_ShouldReturnProjectedData()
        {
            // Arrange
            await _repository.InsertOneAsync(_testDocument);

            // Act
            var results = _repository.FilterBy(
                x => x.Age == 25,
                x => new { x.Name }
            );

            // Assert
            var result = results.First();
            Assert.Equal(_testDocument.Name, result.Name);
        }
    }
}
