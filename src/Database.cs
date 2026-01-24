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
    public ConnectionStringSettings ConnectionStringSettings { get; } = connectionStringSettings ?? throw new ArgumentNullException(nameof(ConnectionStringSettings));

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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DataTable ExecuteDataTable(CommandType commandType, string commandText, DbParameter[]? commandParameters = null)
    {
        using var command = SqlHelper.CreateCommand(GetOrCreateConnection(), _transaction, commandType, commandText, commandParameters);
        return SqlHelper.ExecuteDataTable(command);
    }

    /// <summary>
    /// Asynchronously executes the command and returns the result set as a DataTable
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<DataTable> ExecuteDataTableAsync(CommandType commandType, string commandText, DbParameter[]? commandParameters = null, CancellationToken cancellationToken = default)
    {
        using var command = SqlHelper.CreateCommand(GetOrCreateConnection(), _transaction, commandType, commandText, commandParameters);
        return await SqlHelper.ExecuteDataTableAsync(command, cancellationToken);
    }

    /// <summary>
    /// Executes the command and returns the number of rows affected
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ExecuteNonQuery(CommandType commandType, string commandText, DbParameter[]? commandParameters = null)
    {
        using var command = SqlHelper.CreateCommand(GetOrCreateConnection(), _transaction, commandType, commandText, commandParameters);
        return SqlHelper.ExecuteNonQuery(command);
    }

    /// <summary>
    /// Asynchronously executes the command and returns the number of rows affected
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<int> ExecuteNonQueryAsync(CommandType commandType, string commandText, DbParameter[]? commandParameters = null, CancellationToken cancellationToken = default)
    {
        using var command = SqlHelper.CreateCommand(GetOrCreateConnection(), _transaction, commandType, commandText, commandParameters);
        return await SqlHelper.ExecuteNonQueryAsync(command, cancellationToken);
    }

    /// <summary>
    /// Executes the command and returns a forward-only data reader over the result set
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DbDataReader ExecuteReader(CommandType commandType, string commandText, DbParameter[]? commandParameters = null)
    {
        using var command = SqlHelper.CreateCommand(GetOrCreateConnection(), _transaction, commandType, commandText, commandParameters);
        return SqlHelper.ExecuteReader(command);
    }

    /// <summary>
    /// Asynchronously executes the command and returns a forward-only data reader over the result set.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<DbDataReader> ExecuteReaderAsync(CommandType commandType, string commandText, DbParameter[]? commandParameters = null, CancellationToken cancellationToken = default)
    {
        using var command = SqlHelper.CreateCommand(GetOrCreateConnection(), _transaction, commandType, commandText, commandParameters);
        return await SqlHelper.ExecuteReaderAsync(command, cancellationToken);
    }

    /// <summary>
    /// Executes the command and returns the first column of the first row in the result set
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public object? ExecuteScalar(CommandType commandType, string commandText, DbParameter[]? commandParameters = null)
    {
        using var command = SqlHelper.CreateCommand(GetOrCreateConnection(), _transaction, commandType, commandText, commandParameters);
        return SqlHelper.ExecuteScalar(command);
    }

    /// <summary>
    /// Asynchronously executes the command and returns the first column of the first row in the result set.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<object?> ExecuteScalarAsync(CommandType commandType, string commandText, DbParameter[]? commandParameters = null, CancellationToken cancellationToken = default)
    {
        using var command = SqlHelper.CreateCommand(GetOrCreateConnection(), _transaction, commandType, commandText, commandParameters);
        return await SqlHelper.ExecuteScalarAsync(command, cancellationToken);
    }

    /// <summary>
    /// Begins a database transaction
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken)
    {
        ThrowIfTransactionOrConnectionInProgress();

        GetOrCreateConnection();
        await _connection!.OpenAsync(cancellationToken);
        _transaction = await _connection.BeginTransactionAsync(isolationLevel, cancellationToken);
    }

    /// <summary>
    /// Commits the current transaction
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Commit()
    {
        ArgumentNullException.ThrowIfNull(_transaction);
        _transaction.Commit();
        CleanupTransaction();
    }

    /// <summary>
    /// Asynchronously commits the current transaction
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(_transaction);
        await _transaction.CommitAsync(cancellationToken);
        await CleanupTransactionAsync();
    }

    /// <summary>
    /// Rolls back the current transaction
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Rollback()
    {
        ArgumentNullException.ThrowIfNull(_transaction);
        _transaction.Rollback();
        CleanupTransaction();
    }

    /// <summary>
    /// Asynchronously rolls back the current transaction
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task RollbackAsync(CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(_transaction);
        await _transaction.RollbackAsync(cancellationToken);
        await CleanupTransactionAsync();
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
    {
        return _connection ??= SqlHelper.CreateConnection(ConnectionStringSettings.ConnectionString, ConnectionStringSettings.ProviderInvariantName);
    }

    /// <summary>
    /// Cleans up transaction-related resources by disposing the current transaction
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CleanupTransaction()
    {
        _transaction?.Dispose();
        _transaction = null;
    }

    /// <summary>
    /// Asynchronously cleans up transaction-related resources by disposing the current transaction
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private async Task CleanupTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    /// <summary>
    /// Throws an exception if a transaction is currently active or if the saved connection is already in progress
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfTransactionOrConnectionInProgress()
    {
        if (_transaction != null)
            throw new InvalidOperationException("Transaction is already active.");
        if (_connection != null && _connection.State != ConnectionState.Closed)
            throw new InvalidOperationException("Connection is in progress.");
    }

    #region Disposable Pattern
    /// <summary>
    /// Releases the resources used by this instance.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) { return; }

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
        if (_disposed) { return; }

        if (_transaction != null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }

        if (_connection != null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
    #endregion
}
