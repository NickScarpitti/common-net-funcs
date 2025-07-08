using System.Data;

namespace CommonNetFuncs.Sql.Common;

public interface IDirectQuery
{
    Task<DataTable> GetDataTable(string sql, string connStr, int commandTimeoutSeconds = 30, int maxRetry = 3, CancellationToken cancellationToken = default);

    DataTable GetDataTableSynchronous(string sql, string connStr, int commandTimeoutSeconds = 30, int maxRetry = 3);

    Task<UpdateResult> RunUpdateQuery(string sql, string connStr, int commandTimeoutSeconds = 30, int maxRetry = 3, CancellationToken cancellationToken = default);

    UpdateResult RunUpdateQuerySynchronous(string sql, string connStr, int commandTimeoutSeconds = 30, int maxRetry = 3);

    IAsyncEnumerable<T> GetDataStreamAsync<T>(string sql, string connStr, int commandTimeoutSeconds = 30, int maxRetry = 3, CancellationToken cancellationToken = default) where T : class, new();

    IEnumerable<T> GetDataStreamSynchronous<T>(string sql, string connStr, int commandTimeoutSeconds = 30, int maxRetry = 3, CancellationToken cancellationToken = default) where T : class, new();

    Task<IEnumerable<T>> GetDataDirectAsync<T>(string sql, string connStr, int commandTimeoutSeconds = 30, int maxRetry = 3, CancellationToken cancellationToken = default) where T : class, new();
}
