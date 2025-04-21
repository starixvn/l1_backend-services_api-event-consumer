using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace ApiEventConsumer.Database.Tests
{
    public class GenericTestDocument : Document
    {
        public string Name { get; set; } = string.Empty;
    }

    public class IGenericRepositoryTests
    {
        private readonly Mock<IGenericRepository<GenericTestDocument>> _mockRepository;
        private readonly GenericTestDocument _testDocument;
        private readonly List<GenericTestDocument> _testDocuments;

        public IGenericRepositoryTests()
        {
            _mockRepository = new Mock<IGenericRepository<GenericTestDocument>>();
            
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
            _mockRepository.Setup(x => x.AsQueryable())
                .Returns(_testDocuments.AsQueryable());

            // Act
            var result = _mockRepository.Object.AsQueryable();

            // Assert
            Assert.NotNull(result);
            Assert.IsAssignableFrom<IQueryable<GenericTestDocument>>(result);
        }

        [Fact]
        public void FilterBy_ShouldReturnFilteredDocuments()
        {
            // Arrange
            Expression<Func<GenericTestDocument, bool>> filter = x => x.Name == "Doc 1";
            _mockRepository.Setup(x => x.FilterBy(filter))
                .Returns(_testDocuments.Where(x => x.Name == "Doc 1"));

            // Act
            var result = _mockRepository.Object.FilterBy(filter);

            // Assert
            Assert.Single(result);
            Assert.Equal("Doc 1", result.First().Name);
        }

        [Fact]
        public async Task FilterByAsync_ShouldReturnFilteredDocuments()
        {
            // Arrange
            Expression<Func<GenericTestDocument, bool>> filter = x => x.Name == "Doc 1";
            _mockRepository.Setup(x => x.FilterByAsync(filter))
                .ReturnsAsync(_testDocuments.Where(x => x.Name == "Doc 1"));

            // Act
            var result = await _mockRepository.Object.FilterByAsync(filter);

            // Assert
            Assert.Single(result);
            Assert.Equal("Doc 1", result.First().Name);
        }

        [Fact]
        public void FindOne_ShouldReturnSingleDocument()
        {
            // Arrange
            Expression<Func<GenericTestDocument, bool>> filter = x => x.Name == "Test Document";
            _mockRepository.Setup(x => x.FindOne(filter))
                .Returns(_testDocument);

            // Act
            var result = _mockRepository.Object.FindOne(filter);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Document", result.Name);
        }

        [Fact]
        public async Task FindOneAsync_ShouldReturnSingleDocument()
        {
            // Arrange
            Expression<Func<GenericTestDocument, bool>> filter = x => x.Name == "Test Document";
            _mockRepository.Setup(x => x.FindOneAsync(filter))
                .ReturnsAsync(_testDocument);

            // Act
            var result = await _mockRepository.Object.FindOneAsync(filter);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Document", result.Name);
        }

        [Fact]
        public void FindById_ShouldReturnDocumentById()
        {
            // Arrange
            var id = _testDocument.Id.ToString();
            _mockRepository.Setup(x => x.FindById(id))
                .Returns(_testDocument);

            // Act
            var result = _mockRepository.Object.FindById(id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(id, result.Id.ToString());
        }

        [Fact]
        public async Task InsertOneAsync_ShouldInsertDocument()
        {
            // Arrange
            _mockRepository.Setup(x => x.InsertOneAsync(_testDocument))
                .Returns(Task.CompletedTask);

            // Act & Assert
            await _mockRepository.Object.InsertOneAsync(_testDocument);
            _mockRepository.Verify(x => x.InsertOneAsync(_testDocument), Times.Once);
        }

        [Fact]
        public async Task InsertManyAsync_ShouldInsertMultipleDocuments()
        {
            // Arrange
            _mockRepository.Setup(x => x.InsertManyAsync(_testDocuments))
                .Returns(Task.CompletedTask);

            // Act & Assert
            await _mockRepository.Object.InsertManyAsync(_testDocuments);
            _mockRepository.Verify(x => x.InsertManyAsync(_testDocuments), Times.Once);
        }

        [Fact]
        public async Task DeleteOneAsync_ShouldDeleteDocument()
        {
            // Arrange
            Expression<Func<GenericTestDocument, bool>> filter = x => x.Name == "Test Document";
            _mockRepository.Setup(x => x.DeleteOneAsync(filter))
                .Returns(Task.CompletedTask);

            // Act & Assert
            await _mockRepository.Object.DeleteOneAsync(filter);
            _mockRepository.Verify(x => x.DeleteOneAsync(filter), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateDocument()
        {
            // Arrange
            Expression<Func<GenericTestDocument, bool>> filter = x => x.Name == "Test Document";
            var update = Builders<GenericTestDocument>.Update.Set(x => x.Name, "Updated Name");
            var options = new UpdateOptions();

            _mockRepository.Setup(x => x.UpdateAsync(filter, update, options))
                .Returns(Task.CompletedTask);

            // Act & Assert
            await _mockRepository.Object.UpdateAsync(filter, update, options);
            _mockRepository.Verify(x => x.UpdateAsync(filter, update, options), Times.Once);
        }

        [Fact]
        public void Collection_ShouldReturnMongoCollection()
        {
            // Arrange
            var mockCollection = new Mock<IMongoCollection<GenericTestDocument>>();
            _mockRepository.Setup(x => x.Collection())
                .Returns(mockCollection.Object);

            // Act
            var result = _mockRepository.Object.Collection();

            // Assert
            Assert.NotNull(result);
            Assert.IsAssignableFrom<IMongoCollection<GenericTestDocument>>(result);
        }

        [Fact]
        public void Aggregate_ShouldReturnAggregateFluent()
        {
            // Arrange
            var mockAggregate = new Mock<IAggregateFluent<GenericTestDocument>>();
            _mockRepository.Setup(x => x.Aggregate())
                .Returns(mockAggregate.Object);

            // Act
            var result = _mockRepository.Object.Aggregate();

            // Assert
            Assert.NotNull(result);
            Assert.IsAssignableFrom<IAggregateFluent<GenericTestDocument>>(result);
        }
    }
}
