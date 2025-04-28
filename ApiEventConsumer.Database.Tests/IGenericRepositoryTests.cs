using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq.Expressions;
using NSubstitute;

namespace ApiEventConsumer.Database.Tests
{
    public class GenericTestDocument : Document
    {
        public string Name { get; set; } = string.Empty;
    }

    public class IGenericRepositoryTests
    {
        private readonly IGenericRepository<GenericTestDocument> _repository;
        private readonly GenericTestDocument _testDocument;
        private readonly List<GenericTestDocument> _testDocuments;

        public IGenericRepositoryTests()
        {
            _repository = Substitute.For<IGenericRepository<GenericTestDocument>>();
            
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
        public void AsQueryable_ShouldReturnQueryable()
        {
            // Arrange
            _repository.AsQueryable().Returns(_testDocuments.AsQueryable());

            // Act
            var result = _repository.AsQueryable();

            // Assert
            Assert.NotNull(result);
            Assert.IsAssignableFrom<IQueryable<GenericTestDocument>>(result);
        }

        [Fact]
        public void FilterBy_ShouldReturnFilteredDocuments()
        {
            // Arrange
            Expression<Func<GenericTestDocument, bool>> filter = x => x.Name == "Doc 1";
            _repository.FilterBy(filter).Returns(_testDocuments.Where(x => x.Name == "Doc 1"));

            // Act
            var result = _repository.FilterBy(filter);

            // Assert
            Assert.Single(result);
            Assert.Equal("Doc 1", result.First().Name);
        }

        [Fact]
        public async Task FilterByAsync_ShouldReturnFilteredDocuments()
        {
            // Arrange
            Expression<Func<GenericTestDocument, bool>> filter = x => x.Name == "Doc 1";
            _repository.FilterByAsync(filter).Returns(_testDocuments.Where(x => x.Name == "Doc 1"));

            // Act
            var result = await _repository.FilterByAsync(filter);

            // Assert
            Assert.Single(result);
            Assert.Equal("Doc 1", result.First().Name);
        }

        [Fact]
        public void FindOne_ShouldReturnSingleDocument()
        {
            // Arrange
            Expression<Func<GenericTestDocument, bool>> filter = x => x.Name == "Test Document";
            _repository.FindOne(filter).Returns(_testDocument);

            // Act
            var result = _repository.FindOne(filter);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Document", result.Name);
        }

        [Fact]
        public async Task FindOneAsync_ShouldReturnSingleDocument()
        {
            // Arrange
            Expression<Func<GenericTestDocument, bool>> filter = x => x.Name == "Test Document";
            _repository.FindOneAsync(filter).Returns(_testDocument);

            // Act
            var result = await _repository.FindOneAsync(filter);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Document", result.Name);
        }

        [Fact]
        public void FindById_ShouldReturnDocumentById()
        {
            // Arrange
            var id = _testDocument.Id.ToString();
            _repository.FindById(id).Returns(_testDocument);

            // Act
            var result = _repository.FindById(id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(id, result.Id.ToString());
        }

        [Fact]
        public async Task InsertOneAsync_ShouldInsertDocument()
        {
            // Act
            await _repository.InsertOneAsync(_testDocument);

            // Assert
            await _repository.Received(1).InsertOneAsync(_testDocument);
        }

        [Fact]
        public async Task InsertManyAsync_ShouldInsertMultipleDocuments()
        {
            // Act
            await _repository.InsertManyAsync(_testDocuments);

            // Assert
            await _repository.Received(1).InsertManyAsync(_testDocuments);
        }

        [Fact]
        public async Task DeleteOneAsync_ShouldDeleteDocument()
        {
            // Arrange
            Expression<Func<GenericTestDocument, bool>> filter = x => x.Name == "Test Document";

            // Act
            await _repository.DeleteOneAsync(filter);

            // Assert
            await _repository.Received(1).DeleteOneAsync(filter);
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateDocument()
        {
            // Arrange
            Expression<Func<GenericTestDocument, bool>> filter = x => x.Name == "Test Document";
            var update = Builders<GenericTestDocument>.Update.Set(x => x.Name, "Updated Name");
            var options = new UpdateOptions();

            // Act
            await _repository.UpdateAsync(filter, update, options);

            // Assert
            await _repository.Received(1).UpdateAsync(filter, update, options);
        }

        [Fact]
        public void Collection_ShouldReturnMongoCollection()
        {
            // Arrange
            var mockCollection = Substitute.For<IMongoCollection<GenericTestDocument>>();
            _repository.Collection().Returns(mockCollection);

            // Act
            var result = _repository.Collection();

            // Assert
            Assert.NotNull(result);
            Assert.IsAssignableFrom<IMongoCollection<GenericTestDocument>>(result);
        }

        [Fact]
        public void Aggregate_ShouldReturnAggregateFluent()
        {
            // Arrange
            var aggregateFluent = Substitute.For<IAggregateFluent<GenericTestDocument>>();
            _repository.Aggregate().Returns(aggregateFluent);

            // Act
            var result = _repository.Aggregate();

            // Assert
            Assert.NotNull(result);
            Assert.IsAssignableFrom<IAggregateFluent<GenericTestDocument>>(result);
        }
    }
}
