using ApiEventConsumer.Database.Attributes;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ApiEventConsumer.Database
{
    public interface IDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        [BsonElement("_id")]
        ObjectId Id { get; set; }
        DateTime CreatedAt { get; set; }
        DateTime? UpdatedAt { get; set; }
    }

    public class Document : IDocument
    {
        public ObjectId Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public static string GetCollectionName(Type documentType)
        {
            var attribute = documentType.GetCustomAttributes(
                    typeof(BsonCollectionAttribute),
                    true)
                .FirstOrDefault() as BsonCollectionAttribute;
            return attribute?.CollectionName ?? documentType.Name.ToLower();
        }

        public static string GetDatabaseName(Type documentType)
        {
            var attribute = documentType.GetCustomAttributes(
                    typeof(BsonDatabaseAttribute),
                    true)
                .FirstOrDefault() as BsonDatabaseAttribute;
            return attribute?.DatabaseName ?? "default";
        }
    }
}