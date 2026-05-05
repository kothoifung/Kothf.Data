// Licensed to Kothf under one or more agreements.
// Kothf licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Kothf.Data;

/// <summary>
/// Yet another helper class for working with ADO.NET objects
/// </summary>
public static class SqlHelper
{
    /// <summary>
    /// Creates and initializes a DbCommand instance
    /// </summary>
    /// <remarks>Using either the connection associated with the provided transaction or the provided connection</remarks>
    public static DbCommand CreateCommand(DbConnection? connection, DbTransaction? transaction, CommandType commandType, string commandText, DbParameter[]? commandParameters = null)
    {
        var transactionConnection = transaction?.Connection;

        if (connection != null && transactionConnection != null && !ReferenceEquals(connection, transactionConnection))
            throw new ArgumentException("The specified connection does not match the transaction connection.", nameof(connection));

        var command = (transactionConnection ?? connection)?.CreateCommand()
            ?? throw new InvalidOperationException("Failed to create the command.");

        command.CommandText = commandText;
        command.CommandType = commandType;

        if (transaction != null)
        {
            command.Transaction = transaction;
        }

        if (commandParameters != null)
        {
            command.Parameters.AddRange(commandParameters);
        }

        return command;
    }

    /// <summary>
    /// Creates a provider-specific DbConnection instance
    /// </summary>
    public static DbConnection CreateConnection(string connectionString, string providerInvariantName)
    {
        var connection = DbProviderFactoryCache.GetFactory(providerInvariantName).CreateConnection()
            ?? throw new InvalidOperationException("Failed to create the connection according to the specified provider.");

        connection.ConnectionString = connectionString;
        return connection;
    }

    /// <summary>
    /// Creates a provider-specific DbConnection instance
    /// </summary>
    public static DbConnection CreateConnection(ConnectionStringSettings connectionStringSettings)
        => CreateConnection(connectionStringSettings.ConnectionString, connectionStringSettings.ProviderInvariantName);

    /// <summary>
    /// Creates a provider-specific DbConnection instance and opens it before returning
    /// </summary>
    public static DbConnection CreateConnectionAndOpen(string connectionString, string providerInvariantName)
    {
        var connection = DbProviderFactoryCache.GetFactory(providerInvariantName).CreateConnection()
            ?? throw new InvalidOperationException("Failed to create the connection according to the specified provider.");

        connection.ConnectionString = connectionString;
        connection.Open();

        return connection;
    }

    /// <summary>
    /// Creates a provider-specific DbConnection instance and opens it before returning
    /// </summary>
    public static DbConnection CreateConnectionAndOpen(ConnectionStringSettings connectionStringSettings)
        => CreateConnection(connectionStringSettings.ConnectionString, connectionStringSettings.ProviderInvariantName);

    /// <summary>
    /// Creates a provider-specific DbConnection instance and asynchronously opens it before returning
    /// </summary>
    public static async Task<DbConnection> CreateConnectionAndOpenAsync(string connectionString, string providerInvariantName, CancellationToken cancellationToken = default)
    {
        var connection = DbProviderFactoryCache.GetFactory(providerInvariantName).CreateConnection()
            ?? throw new InvalidOperationException("Failed to create the connection according to the specified provider.");

        connection.ConnectionString = connectionString;
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        return connection;
    }

    /// <summary>
    /// Creates a provider-specific DbConnection instance and asynchronously opens it before returning
    /// </summary>
    public static Task<DbConnection> CreateConnectionAndOpenAsync(ConnectionStringSettings connectionStringSettings, CancellationToken cancellationToken = default)
        => CreateConnectionAndOpenAsync(connectionStringSettings.ConnectionString, connectionStringSettings.ProviderInvariantName, cancellationToken);

    /// <summary>
    /// Creates a provider-specific DbParameter instance
    /// </summary>
    public static DbParameter CreateParameter<T>(string providerInvariantName, string parameterName, DbType type, T? value = default)
    {
        var parameter = DbProviderFactoryCache.GetFactory(providerInvariantName).CreateParameter()
            ?? throw new InvalidOperationException("Failed to create the parameter according to the specified provider.");

        parameter.ParameterName = parameterName;
        parameter.DbType = type;
        parameter.Value = value is null ? DBNull.Value : value;

        return parameter;
    }

    /// <summary>
    /// Creates a provider-specific DbParameter instance
    /// </summary>
    public static DbParameter CreateParameter<T>(ConnectionStringSettings connectionStringSettings, string parameterName, DbType type, T? value = default)
        => CreateParameter(connectionStringSettings.ProviderInvariantName, parameterName, type, value);

    /// <summary>
    /// Executes the command and returns the result set as a DataTable
    /// </summary>
    public static DataTable ExecuteDataTable(DbCommand command)
    {
        using var reader = ExecuteReader(command);

        var dt = BuildDataTableSchemaFromReader(reader);

        dt.BeginLoadData();
        try
        {
            object[] values = new object[dt.Columns.Count];
            while (reader.Read())
            {
                reader.GetValues(values);
                dt.Rows.Add(values);
            }
        }
        finally
        {
            dt.EndLoadData();
        }

        return dt;
    }

    /// <summary>
    /// Asynchronously executes the command and returns the result set as a DataTable
    /// </summary>
    public static async Task<DataTable> ExecuteDataTableAsync(DbCommand command, CancellationToken cancellationToken = default)
    {
        await using var reader = await ExecuteReaderAsync(command, cancellationToken).ConfigureAwait(false);

        var dt = BuildDataTableSchemaFromReader(reader);

        dt.BeginLoadData();
        try
        {
            object[] values = new object[dt.Columns.Count];
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                reader.GetValues(values);
                dt.Rows.Add(values);
            }
        }
        finally
        {
            dt.EndLoadData();
        }

        return dt;
    }

    /// <summary>
    /// Executes the command and returns the number of rows affected
    /// </summary>
    public static int ExecuteNonQuery(DbCommand command)
    {
        bool shouldClose = false;

        try
        {
            shouldClose = TryOpenConnection(command.Connection);
            return command.ExecuteNonQuery();
        }
        finally
        {
            TryCloseConnection(command.Connection, shouldClose);
        }
    }

    /// <summary>
    /// Asynchronously executes the command and returns the number of rows affected
    /// </summary>
    public static async Task<int> ExecuteNonQueryAsync(DbCommand command, CancellationToken cancellationToken = default)
    {
        bool shouldClose = false;

        try
        {
            shouldClose = await TryOpenConnectionAsync(command.Connection, cancellationToken).ConfigureAwait(false);
            return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await TryCloseConnectionAsync(command.Connection, shouldClose).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Executes the command and returns a forward-only data reader over the result set
    /// </summary>
    /// <remarks>If the connection of command was not opened yet, the caller must dispose the returned reader to avoid leaking an open connection.</remarks>
    public static DbDataReader ExecuteReader(DbCommand command)
    {
        bool shouldClose = false;

        try
        {
            shouldClose = TryOpenConnection(command.Connection);
            return command.ExecuteReader(shouldClose ? CommandBehavior.CloseConnection : CommandBehavior.Default);
        }
        catch (Exception)
        {
            TryCloseConnection(command.Connection, shouldClose);
            throw;
        }
    }

    /// <summary>
    /// Asynchronously executes the command and returns a forward-only data reader over the result set.
    /// </summary>
    /// <remarks>If the connection of command was not opened yet, the caller must dispose the returned reader to avoid leaking an open connection.</remarks>
    public static async Task<DbDataReader> ExecuteReaderAsync(DbCommand command, CancellationToken cancellationToken = default)
    {
        bool shouldClose = false;

        try
        {
            shouldClose = await TryOpenConnectionAsync(command.Connection, cancellationToken).ConfigureAwait(false);
            return await command.ExecuteReaderAsync(shouldClose ? CommandBehavior.CloseConnection : CommandBehavior.Default, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            await TryCloseConnectionAsync(command.Connection, shouldClose).ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Executes the command and returns the first column of the first row in the result set
    /// </summary>
    public static object? ExecuteScalar(DbCommand command)
    {
        bool shouldClose = false;

        try
        {
            shouldClose = TryOpenConnection(command.Connection);
            return command.ExecuteScalar();
        }
        finally
        {
            TryCloseConnection(command.Connection, shouldClose);
        }
    }

    /// <summary>
    /// Asynchronously executes the command and returns the first column of the first row in the result set.
    /// </summary>
    public static async Task<object?> ExecuteScalarAsync(DbCommand command, CancellationToken cancellationToken = default)
    {
        bool shouldClose = false;

        try
        {
            shouldClose = await TryOpenConnectionAsync(command.Connection, cancellationToken).ConfigureAwait(false);
            return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await TryCloseConnectionAsync(command.Connection, shouldClose).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Reinitializes an existing DbCommand so it can be executed again with new text/type/parameters
    /// </summary>
    /// <param name="command">The command instance to reuse</param>
    /// <returns>The same command instance, updated with the provided values.</returns>
    public static DbCommand ReuseCommand(DbCommand command, CommandType commandType, string commandText, DbParameter[]? commandParameters)
    {
        ArgumentNullException.ThrowIfNull(command);

        command.CommandText = commandText;
        command.CommandType = commandType;

        command.Parameters.Clear();

        if (commandParameters != null)
        {
            command.Parameters.AddRange(commandParameters);
        }

        return command;
    }

    /// <summary>
    /// Ensures the specified connection is open, opening it only when necessary.
    /// </summary>
    /// <returns>True if the connection was opened by this method; otherwise, false.</returns>
    private static bool TryOpenConnection(DbConnection? connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        if (connection.State == ConnectionState.Broken)
        {
            connection.Close();
        }

        if (connection.State == ConnectionState.Closed)
        {
            connection.Open();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Ensures the specified connection is open, opening it asynchronously only when necessary.
    /// </summary>
    /// <returns>True if the connection was opened by this method; otherwise, false.</returns> 
    private static async Task<bool> TryOpenConnectionAsync(DbConnection? connection, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        if (connection.State == ConnectionState.Broken)
        {
            await connection.CloseAsync().ConfigureAwait(false);
        }

        if (connection.State == ConnectionState.Closed)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Closes the connection only if it was opened by the caller previously and is not already closed
    /// </summary>
    private static void TryCloseConnection(DbConnection? connection, bool shouldClose)
    {
        if (shouldClose &&
            connection != null && connection.State != ConnectionState.Closed)
        {
            connection.Close();
        }
    }

    /// <summary>
    /// Asynchronously closes the connection only if it was opened by the caller previously and is not already closed
    /// </summary>
    private static async Task TryCloseConnectionAsync(DbConnection? connection, bool shouldClose)
    {
        if (shouldClose &&
            connection != null && connection.State != ConnectionState.Closed)
        {
            await connection.CloseAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Builds a DataTable schema based on the metadata of the provided data reader, without loading any data.
    /// </summary>
    /// <param name="reader">A data reader</param>
    /// <returns>A DataTable representing the schema of the data reader</returns>
    private static DataTable BuildDataTableSchemaFromReader(DbDataReader reader)
    {
        var dt = new DataTable();
        int fieldCount = reader.FieldCount;

        for (int index = 0; index < fieldCount; index++)
        {
            string columnName = reader.GetName(index);
            if (string.IsNullOrEmpty(columnName))
            {
                columnName = $"Column{index}";
            }

            if (dt.Columns.Contains(columnName))
            {
                int suffix = 1;
                string baseName = columnName;
                do
                {
                    columnName = $"{baseName}_{suffix++}";
                }
                while (dt.Columns.Contains(columnName));
            }

            Type columnType = reader.GetFieldType(index);
            dt.Columns.Add(columnName, Nullable.GetUnderlyingType(columnType) ?? columnType);
        }

        return dt;
    }
}
