// Licensed to Kothf under one or more agreements.
// Kothf licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace Kothf.Data;

/// <summary>
/// A Database object
/// </summary>
/// <param name="connectionStringSettings">The settings required to establish a database connection</param>
public class Database(ConnectionStringSettings connectionStringSettings) : IDatabase, IAsyncDisposable, IDisposable
{
    protected DbConnection? _connection;
    protected DbTransaction? _transaction;
    private bool _disposed;

    /// <summary>
    /// The settings required to establish a database connection
    /// </summary>
    public ConnectionStringSettings ConnectionStringSettings { get; } = connectionStringSettings ?? throw new ArgumentNullException(nameof(connectionStringSettings));

    /// <summary>
    /// Indicates whether a transaction is currently active
    /// </summary>
    public bool InTransaction => _transaction != null;

    /// <summary>
    /// Creates a DbParameter instance
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DbParameter CreateParameter<T>(string parameterName, DbType type, T? value) => SqlHelper.CreateParameter(ConnectionStringSettings.ProviderInvariantName, parameterName, type, value);

    /// <summary>
    /// Executes the command and returns the result set as a DataTable
    /// </summary>
    public DataTable ExecuteDataTable(CommandType commandType, string commandText, DbParameter[]? commandParameters = null)
    {
        using var command = SqlHelper.CreateCommand(GetOrCreateConnection(), _transaction, commandType, commandText, commandParameters);
        return SqlHelper.ExecuteDataTable(command);
    }

    /// <summary>
    /// Asynchronously executes the command and returns the result set as a DataTable
    /// </summary>
    public async Task<DataTable> ExecuteDataTableAsync(CommandType commandType, string commandText, DbParameter[]? commandParameters = null, CancellationToken cancellationToken = default)
    {
        using var command = SqlHelper.CreateCommand(GetOrCreateConnection(), _transaction, commandType, commandText, commandParameters);
        return await SqlHelper.ExecuteDataTableAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the command and returns the number of rows affected
    /// </summary>
    public int ExecuteNonQuery(CommandType commandType, string commandText, DbParameter[]? commandParameters = null)
    {
        using var command = SqlHelper.CreateCommand(GetOrCreateConnection(), _transaction, commandType, commandText, commandParameters);
        return SqlHelper.ExecuteNonQuery(command);
    }

    /// <summary>
    /// Asynchronously executes the command and returns the number of rows affected
    /// </summary>
    public async Task<int> ExecuteNonQueryAsync(CommandType commandType, string commandText, DbParameter[]? commandParameters = null, CancellationToken cancellationToken = default)
    {
        using var command = SqlHelper.CreateCommand(GetOrCreateConnection(), _transaction, commandType, commandText, commandParameters);
        return await SqlHelper.ExecuteNonQueryAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the command and returns the numbers of rows in the result set
    /// </summary>
    /// <remarks>
    /// A forward-only data reader over the result set will be used internally.
    /// Please ensure to supply a result set handler to consume the reader.
    /// </remarks>
    public List<T> ExecuteReader<T>(CommandType commandType, string commandText, Func<DbDataReader, List<T>> resultSetHandler, DbParameter[]? commandParameters = null)
    {
        ArgumentNullException.ThrowIfNull(resultSetHandler);

        using var command = SqlHelper.CreateCommand(GetOrCreateConnection(), _transaction, commandType, commandText, commandParameters);
        using var reader = SqlHelper.ExecuteReader(command);

        var result = resultSetHandler(reader);

        return result;
    }

    /// <summary>
    /// Executes the command and returns the numbers of rows in the result set
    /// </summary>
    /// <remarks>
    /// A forward-only data reader over the result set will be used internally.
    /// Please ensure to supply a record handler to consume the reader.
    /// </remarks>
    public List<T> ExecuteReader<T>(CommandType commandType, string commandText, Func<DbDataReader, T> recordHandler, DbParameter[]? commandParameters = null)
    {
        ArgumentNullException.ThrowIfNull(recordHandler);

        using var command = SqlHelper.CreateCommand(GetOrCreateConnection(), _transaction, commandType, commandText, commandParameters);
        using var reader = SqlHelper.ExecuteReader(command);

        var result = new List<T>();

        while (reader.Read())
        {
            var record = recordHandler(reader);
            result.Add(record);
        }

        return result;
    }

    /// <summary>
    /// Asynchronously executes the command and returns a forward-only data reader over the result set.
    /// </summary>
    /// <remarks>
    /// A forward-only data reader over the result set will be used internally.
    /// Please ensure to supply a result set handler to consume the reader.
    /// </remarks>
    public async Task<List<T>> ExecuteReaderAsync<T>(CommandType commandType, string commandText, Func<DbDataReader, CancellationToken, Task<List<T>>> resultSetHandler, DbParameter[]? commandParameters = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resultSetHandler);

        await using var command = SqlHelper.CreateCommand(GetOrCreateConnection(), _transaction, commandType, commandText, commandParameters);
        await using var reader = await SqlHelper.ExecuteReaderAsync(command, cancellationToken).ConfigureAwait(false);

        var result = await resultSetHandler(reader, cancellationToken).ConfigureAwait(false);

        return result;
    }

    /// <summary>
    /// Asynchronously executes the command and returns a forward-only data reader over the result set.
    /// </summary>
    /// <remarks>
    /// A forward-only data reader over the result set will be used internally.
    /// Please ensure to supply a record handler to consume the reader.
    /// </remarks>
    public async Task<List<T>> ExecuteReaderAsync<T>(CommandType commandType, string commandText, Func<DbDataReader, CancellationToken, Task<T>> recordHandler, DbParameter[]? commandParameters = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(recordHandler);

        await using var command = SqlHelper.CreateCommand(GetOrCreateConnection(), _transaction, commandType, commandText, commandParameters);
        await using var reader = await SqlHelper.ExecuteReaderAsync(command, cancellationToken).ConfigureAwait(false);

        var result = new List<T>();

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var record = await recordHandler(reader, cancellationToken).ConfigureAwait(false);
            result.Add(record);
        }

        return result;
    }

    /// <summary>
    /// Executes the command and returns the first column of the first row in the result set
    /// </summary>
    public object? ExecuteScalar(CommandType commandType, string commandText, DbParameter[]? commandParameters = null)
    {
        using var command = SqlHelper.CreateCommand(GetOrCreateConnection(), _transaction, commandType, commandText, commandParameters);
        return SqlHelper.ExecuteScalar(command);
    }

    /// <summary>
    /// Asynchronously executes the command and returns the first column of the first row in the result set.
    /// </summary>
    public async Task<object?> ExecuteScalarAsync(CommandType commandType, string commandText, DbParameter[]? commandParameters = null, CancellationToken cancellationToken = default)
    {
        using var command = SqlHelper.CreateCommand(GetOrCreateConnection(), _transaction, commandType, commandText, commandParameters);
        return await SqlHelper.ExecuteScalarAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Begins a database transaction
    /// </summary>
    public void BeginTransaction(IsolationLevel isolationLevel)
    {
        ThrowIfTransactionOrConnectionInProgress();

        GetOrCreateConnection();
        _connection!.Open();
        _transaction = _connection!.BeginTransaction(isolationLevel);
    }

    /// <summary>
    /// Asynchronously begins a database transaction
    /// </summary>
    public async Task BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken)
    {
        ThrowIfTransactionOrConnectionInProgress();

        GetOrCreateConnection();
        await _connection!.OpenAsync(cancellationToken).ConfigureAwait(false);
        _transaction = await _connection.BeginTransactionAsync(isolationLevel, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Commits the current transaction
    /// </summary>
    public void Commit()
    {
        ArgumentNullException.ThrowIfNull(_transaction);
        _transaction.Commit();
        CleanupTransaction();
    }

    /// <summary>
    /// Asynchronously commits the current transaction
    /// </summary>
    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(_transaction);
        await _transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        await CleanupTransactionAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Rolls back the current transaction
    /// </summary>
    public void Rollback()
    {
        ArgumentNullException.ThrowIfNull(_transaction);
        _transaction.Rollback();
        CleanupTransaction();
    }

    /// <summary>
    /// Asynchronously rolls back the current transaction
    /// </summary>
    public async Task RollbackAsync(CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(_transaction);
        await _transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
        await CleanupTransactionAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the saved DbConnection, creating it if necessary.
    /// </summary>
    /// <remarks>
    /// This method implements lazy initialization for the underlying connection.
    /// The created connection is saved in _connection and reused on subsequent calls.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected DbConnection GetOrCreateConnection()
        => _connection ??= SqlHelper.CreateConnection(ConnectionStringSettings.ConnectionString, ConnectionStringSettings.ProviderInvariantName);

    /// <summary>
    /// Cleans up transaction-related resources by disposing the current transaction
    /// </summary>
    private void CleanupTransaction()
    {
        _transaction?.Dispose();
        _transaction = null;

        // Close the connection for it's always been opened in BeginTransaction.
        _connection?.Close();
    }

    /// <summary>
    /// Asynchronously cleans up transaction-related resources by disposing the current transaction
    /// </summary>
    private async Task CleanupTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.DisposeAsync().ConfigureAwait(false);
            _transaction = null;
        }

        // Close the connection for it's always been opened in BeginTransaction.
        if (_connection != null)
        {
            await _connection.CloseAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Throws an exception if a transaction is currently active or if the saved connection is already in progress
    /// </summary>
    private void ThrowIfTransactionOrConnectionInProgress()
    {
        if (_transaction != null)
            throw new InvalidOperationException("Transaction is already active.");
        if (_connection != null && _connection.State != ConnectionState.Closed)
            throw new InvalidOperationException("Connection is in progress.");
    }

    #region IDisposable & AsyncDisposable Implementation
    /// <summary>
    /// Releases the resources used by this instance.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        { return; }

        _transaction?.Dispose();
        _transaction = null;

        _connection?.Dispose();
        _connection = null;

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Asynchronously releases the resources used by this instance.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        { return; }

        if (_transaction != null)
        {
            await _transaction.DisposeAsync().ConfigureAwait(false);
            _transaction = null;
        }

        if (_connection != null)
        {
            await _connection.DisposeAsync().ConfigureAwait(false);
            _connection = null;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
    #endregion
}
