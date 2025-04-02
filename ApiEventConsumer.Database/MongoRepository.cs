using System.Linq.Expressions;
using MongoDB.Driver;
using MongoDB.Bson;

namespace ApiEventConsumer.Database
{
    public class MongoRepository<TDocument> : IGenericRepository<TDocument> where TDocument : IDocument
    {
        private readonly IMongoCollection<TDocument> _collection;

        public MongoRepository(IMongoDatabase database)
        {
            var collectionName = Document.GetCollectionName(typeof(TDocument));
            _collection = database.GetCollection<TDocument>(collectionName);
        }

        public IQueryable<TDocument> AsQueryable()
        {
            return _collection.AsQueryable();
        }

        public IEnumerable<TDocument> FilterBy(Expression<Func<TDocument, bool>> filterExpression)
        {
            return _collection.Find(filterExpression).ToEnumerable();
        }

        public IEnumerable<TProjected> FilterBy<TProjected>(Expression<Func<TDocument, bool>> filterExpression, Expression<Func<TDocument, TProjected>> projectionExpression)
        {
            return _collection.Find(filterExpression).Project(projectionExpression).ToEnumerable();
        }

        public async Task<IEnumerable<TDocument>> FilterByAsync(Expression<Func<TDocument, bool>> filterExpression)
        {
            return await _collection.Find(filterExpression).ToListAsync();
        }

        public TDocument FindOne(Expression<Func<TDocument, bool>> filterExpression)
        {
            return _collection.Find(filterExpression).FirstOrDefault();
        }

        public async Task<TDocument> FindOneAsync(Expression<Func<TDocument, bool>> filterExpression)
        {
            return await _collection.Find(filterExpression).FirstOrDefaultAsync();
        }

        public TDocument FindById(string id)
        {
            var objectId = ObjectId.Parse(id);
            var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, objectId);
            return _collection.Find(filter).SingleOrDefault();
        }

        public async Task<TDocument> FindByIdAsync(string id)
        {
            var objectId = ObjectId.Parse(id);
            var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, objectId);
            return await _collection.Find(filter).SingleOrDefaultAsync();
        }

        public void InsertOne(TDocument document)
        {
            _collection.InsertOne(document);
        }

        public async Task InsertOneAsync(TDocument document)
        {
            await _collection.InsertOneAsync(document);
        }

        public void InsertMany(IEnumerable<TDocument> documents)
        {
            _collection.InsertMany(documents);
        }

        public async Task InsertManyAsync(IEnumerable<TDocument> documents)
        {
            await _collection.InsertManyAsync(documents);
        }

        public void ReplaceOne(TDocument document)
        {
            var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, document.Id);
            _collection.FindOneAndReplace(filter, document);
        }

        public async Task ReplaceOneAsync(TDocument document)
        {
            var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, document.Id);
            await _collection.FindOneAndReplaceAsync(filter, document);
        }

        public void DeleteOne(Expression<Func<TDocument, bool>> filterExpression)
        {
            _collection.FindOneAndDelete(filterExpression);
        }

        public async Task DeleteOneAsync(Expression<Func<TDocument, bool>> filterExpression)
        {
            await _collection.FindOneAndDeleteAsync(filterExpression);
        }

        public void DeleteById(string id)
        {
            var objectId = ObjectId.Parse(id);
            var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, objectId);
            _collection.FindOneAndDelete(filter);
        }

        public async Task DeleteByIdAsync(string id)
        {
            var objectId = ObjectId.Parse(id);
            var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, objectId);
            await _collection.FindOneAndDeleteAsync(filter);
        }

        public void DeleteMany(Expression<Func<TDocument, bool>> filterExpression)
        {
            _collection.DeleteMany(filterExpression);
        }

        public async Task DeleteManyAsync(Expression<Func<TDocument, bool>> filterExpression)
        {
            await _collection.DeleteManyAsync(filterExpression);
        }

        public void Update(Expression<Func<TDocument, bool>> filterExpression, UpdateDefinition<TDocument> updateDefinition, UpdateOptions options = null)
        {
            _collection.UpdateOne(filterExpression, updateDefinition, options);
        }

        public async Task UpdateAsync(Expression<Func<TDocument, bool>> filterExpression, UpdateDefinition<TDocument> updateDefinition, UpdateOptions options = null)
        {
            await _collection.UpdateOneAsync(filterExpression, updateDefinition, options);
        }

        public async Task UpdateManyAsync(Expression<Func<TDocument, bool>> filterExpression, UpdateDefinition<TDocument> updateDefinition, UpdateOptions options = null)
        {
            await _collection.UpdateManyAsync(filterExpression, updateDefinition, options);
        }

        public IAggregateFluent<TDocument> Aggregate()
        {
            return _collection.Aggregate();
        }

        public IMongoCollection<TDocument> Collection()
        {
            return _collection;
        }
    }
} 