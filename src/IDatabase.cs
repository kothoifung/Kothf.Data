// Licensed to Kothf under one or more agreements.
// Kothf licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data;
using System.Data.Common;

namespace Kothf.Data;

/// <summary>
/// The interface for a Database object
/// </summary>
public interface IDatabase : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Gets the settings required to establish a database connection
    /// </summary>
    ConnectionStringSettings ConnectionStringSettings { get; }

    /// <summary>
    /// Indicates whether a transaction is currently active
    /// </summary>
    bool InTransaction { get; }

    /// <summary>
    /// Creates a provider-specific database parameter.
    /// </summary>
    /// <typeparam name="T">The value type of the parameter.</typeparam>
    DbParameter CreateParameter<T>(string parameterName, DbType type, T? value = default);

    /// <summary>
    /// Executes the command and returns the result set as a DataTable
    /// </summary>
    DataTable ExecuteDataTable(CommandType commandType, string commandText, DbParameter[]? commandParameters = null);

    /// <summary>
    /// Asynchronously executes the command and returns the result set as a DataTable
    /// </summary>
    Task<DataTable> ExecuteDataTableAsync(CommandType commandType, string commandText, DbParameter[]? commandParameters = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the command and returns the number of rows affected
    /// </summary>
    int ExecuteNonQuery(CommandType commandType, string commandText, DbParameter[]? commandParameters = null);

    /// <summary>
    /// Asynchronously executes the command and returns the number of rows affected
    /// </summary>
    Task<int> ExecuteNonQueryAsync(CommandType commandType, string commandText, DbParameter[]? commandParameters = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the command and returns a forward-only data reader over the result set
    /// </summary>
    DbDataReader ExecuteReader(CommandType commandType, string commandText, DbParameter[]? commandParameters = null);

    /// <summary>
    /// Asynchronously executes the command and returns a forward-only data reader over the result set.
    /// </summary>
    Task<DbDataReader> ExecuteReaderAsync(CommandType commandType, string commandText, DbParameter[]? commandParameters = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the command and returns the first column of the first row in the result set
    /// </summary>
    object? ExecuteScalar(CommandType commandType, string commandText, DbParameter[]? commandParameters = null);

    /// <summary>
    /// Asynchronously executes the command and returns the first column of the first row in the result set.
    /// </summary>
    Task<object?> ExecuteScalarAsync(CommandType commandType, string commandText, DbParameter[]? commandParameters = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a database transaction
    /// </summary>
    void BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.Unspecified);

    /// <summary>
    /// Asynchronously begins a database transaction
    /// </summary>
    Task BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.Unspecified, CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current transaction
    /// </summary>
    void Commit();

    /// <summary>
    /// Asynchronously commits the current transaction
    /// </summary>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the current transaction
    /// </summary>
    void Rollback();

    /// <summary>
    /// Asynchronously rolls back the current transaction
    /// </summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
