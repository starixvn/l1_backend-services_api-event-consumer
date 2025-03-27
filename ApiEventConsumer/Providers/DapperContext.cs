using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Polly;

namespace ApiEventConsumer.Providers
{
    public interface IDapperContext
    {
        IDbConnection CreateConnection();

        Task<IEnumerable<T>> QueryAsync<T>(string procedure, object param = null, CommandType? commandType = null);
    }

    public class DapperContext : IDapperContext
    {
        private const int MaxRetryAttempts = 3;
        private readonly string DbConn;
        private readonly ISentryClient sentryClient;

        public DapperContext(ProviderSettings settings, ISentryClient sentryClient)
        {
            DbConn = settings.ConnectionString;
            this.sentryClient = sentryClient;
        }

        public IDbConnection CreateConnection()
            => new SqlConnection(DbConn);

        public async Task<IEnumerable<T>> QueryAsync<T>(string procedure, object param = null, CommandType? commandType = null)
        {
            var policy = Policy
              .Handle<SqlException>()
              .WaitAndRetryAsync(
                MaxRetryAttempts,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (exception, timeSpan, context) =>
                {
                    sentryClient.CaptureMessage($"Failed to get high stake. Exception {exception.Message}. Next retry is {timeSpan}.", SentryLevel.Error);
                }
              );

            return await policy.ExecuteAsync(async () => await ExcuteQueryAsync<T>(procedure, param, commandType));
        }

        private async Task<IEnumerable<T>> ExcuteQueryAsync<T>(string procedure, object param = null, CommandType? commandType = null)
        {
            using var connection = CreateConnection();

            return await connection.QueryAsync<T>(procedure, param, commandType: commandType);
        }
    }
}