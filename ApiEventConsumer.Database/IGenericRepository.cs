using System.Linq.Expressions;
using MongoDB.Driver;

namespace ApiEventConsumer.Database
{
    public interface IGenericRepository<TDocument> where TDocument : IDocument
    {
        IQueryable<TDocument> AsQueryable();

        IEnumerable<TDocument> FilterBy(
            Expression<Func<TDocument, bool>> filterExpression);

        IEnumerable<TProjected> FilterBy<TProjected>(
            Expression<Func<TDocument, bool>> filterExpression,
            Expression<Func<TDocument, TProjected>> projectionExpression);

        Task<IEnumerable<TDocument>> FilterByAsync(
           Expression<Func<TDocument, bool>> filterExpression);

        TDocument FindOne(Expression<Func<TDocument, bool>> filterExpression);

        Task<TDocument> FindOneAsync(Expression<Func<TDocument, bool>> filterExpression);

        TDocument FindById(string id);

        Task<TDocument> FindByIdAsync(string id);

        void InsertOne(TDocument document);

        Task InsertOneAsync(TDocument document);

        void InsertMany(IEnumerable<TDocument> documents);

        Task InsertManyAsync(IEnumerable<TDocument> documents);

        void ReplaceOne(TDocument document);

        Task ReplaceOneAsync(TDocument document);

        void DeleteOne(Expression<Func<TDocument, bool>> filterExpression);

        Task DeleteOneAsync(Expression<Func<TDocument, bool>> filterExpression);

        void DeleteById(string id);

        Task DeleteByIdAsync(string id);

        void DeleteMany(Expression<Func<TDocument, bool>> filterExpression);

        Task DeleteManyAsync(Expression<Func<TDocument, bool>> filterExpression);

        void Update(Expression<Func<TDocument, bool>> filterExpression, UpdateDefinition<TDocument> updateDefinition, UpdateOptions? options = null);

        Task UpdateAsync(Expression<Func<TDocument, bool>> filterExpression, UpdateDefinition<TDocument> updateDefinition, UpdateOptions? options = null);

        Task UpdateManyAsync(Expression<Func<TDocument, bool>> filterExpression, UpdateDefinition<TDocument> updateDefinition, UpdateOptions? options = null);

        IAggregateFluent<TDocument> Aggregate();

        IMongoCollection<TDocument> Collection();
    }
}