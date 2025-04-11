using ApiEventConsumer.Database.Attributes;
using MongoDB.Bson;

namespace ApiEventConsumer.Database.Tests
{
    [BsonCollection("test_collection")]
    [BsonDatabase("test_database")]
    public class TestDocument : Document
    {
    }

    [BsonCollection("another_collection")]
    public class AnotherTestDocument : Document
    {
    }

    public class DocumentWithoutAttributes : Document
    {
    }

    public class IDocumentTests
    {
        [Fact]
        public void Document_Properties_ShouldSetAndGetCorrectly()
        {
            // Arrange
            var doc = new TestDocument
            {
                Id = ObjectId.GenerateNewId(),
                CreatedAt = new DateTime(2024, 1, 1),
                UpdatedAt = new DateTime(2024, 1, 2)
            };

            // Assert
            Assert.NotEqual(ObjectId.Empty, doc.Id);
            Assert.Equal(new DateTime(2024, 1, 1), doc.CreatedAt);
            Assert.Equal(new DateTime(2024, 1, 2), doc.UpdatedAt);
        }

        [Fact]
        public void Document_DefaultValues_ShouldBeCorrect()
        {
            // Arrange
            var doc = new TestDocument();

            // Assert
            Assert.Equal(ObjectId.Empty, doc.Id);
            Assert.Equal(default(DateTime), doc.CreatedAt);
            Assert.Null(doc.UpdatedAt);
        }

        [Fact]
        public void GetCollectionName_WithAttribute_ShouldReturnCorrectName()
        {
            // Act
            var collectionName = Document.GetCollectionName(typeof(TestDocument));

            // Assert
            Assert.Equal("test_collection", collectionName);
        }

        [Fact]
        public void GetCollectionName_WithoutAttribute_ShouldReturnTypeName()
        {
            // Act
            var collectionName = Document.GetCollectionName(typeof(DocumentWithoutAttributes));

            // Assert
            Assert.Equal("documentwithoutattributes", collectionName);
        }

        [Fact]
        public void GetDatabaseName_WithAttribute_ShouldReturnCorrectName()
        {
            // Act
            var databaseName = Document.GetDatabaseName(typeof(TestDocument));

            // Assert
            Assert.Equal("test_database", databaseName);
        }

        [Fact]
        public void GetDatabaseName_WithoutAttribute_ShouldReturnDefault()
        {
            // Act
            var databaseName = Document.GetDatabaseName(typeof(DocumentWithoutAttributes));

            // Assert
            Assert.Equal("default", databaseName);
        }

        [Fact]
        public void GetCollectionName_WithDifferentDocuments_ShouldReturnCorrectNames()
        {
            // Act
            var testCollection = Document.GetCollectionName(typeof(TestDocument));
            var anotherCollection = Document.GetCollectionName(typeof(AnotherTestDocument));

            // Assert
            Assert.Equal("test_collection", testCollection);
            Assert.Equal("another_collection", anotherCollection);
        }

        [Theory]
        [InlineData(typeof(TestDocument), "test_collection")]
        [InlineData(typeof(AnotherTestDocument), "another_collection")]
        [InlineData(typeof(DocumentWithoutAttributes), "documentwithoutattributes")]
        public void GetCollectionName_ShouldHandleVariousTypes(Type documentType, string expectedName)
        {
            // Act
            var collectionName = Document.GetCollectionName(documentType);

            // Assert
            Assert.Equal(expectedName, collectionName);
        }

        [Fact]
        public void Document_ShouldImplementIDocument()
        {
            // Arrange
            var doc = new TestDocument();

            // Assert
            Assert.IsAssignableFrom<IDocument>(doc);
        }

        [Fact]
        public void ObjectId_ShouldGenerateUniqueValues()
        {
            // Arrange
            var doc1 = new TestDocument { Id = ObjectId.GenerateNewId() };
            var doc2 = new TestDocument { Id = ObjectId.GenerateNewId() };

            // Assert
            Assert.NotEqual(doc1.Id, doc2.Id);
        }
    }
}
