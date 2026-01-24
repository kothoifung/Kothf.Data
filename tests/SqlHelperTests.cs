using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Transactions;
using Microsoft.Extensions.Logging;

namespace Kothf.Data.Tests;

/// <summary>
/// Test of <see cref="SqlHelper"/>
/// </summary>
internal sealed class SqlHelperTests
{
    private readonly ConnectionStringSettings _connectionStringSettings;
    private readonly ILogger<SqlHelperTests> _logger;

    public SqlHelperTests(ConnectionStringSettings connectionStringSettings, ILogger<SqlHelperTests> logger)
    {
        _connectionStringSettings = connectionStringSettings;
        _logger = logger;
    }

    private string ConnectionString => _connectionStringSettings.ConnectionString;

    private string ProviderInvariantName => _connectionStringSettings.ProviderInvariantName;

    public async Task ExecuteReaderAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Start to test `ExecuteReaderAsync`");

        try
        {
            List<User> modelList = [];

            DbParameter[] parameters = [
                SqlHelper.CreateParameter(ProviderInvariantName, "@p1", DbType.Int32, 1)
            ];

            string sql = $$"""
            SELECT
                UserCode,UserName,Password,Email,Phone,Attributes,[State],CreatedTime,LastModifiedTime
                FROM [User]
                WHERE [State] = @p1;
            """;

            using var connection = SqlHelper.CreateConnection(ConnectionString, ProviderInvariantName);
            using var command = SqlHelper.CreateCommand(connection, null, CommandType.Text, sql, parameters);
            using var reader = await SqlHelper.ExecuteReaderAsync(command, stoppingToken);

            while (await reader.ReadAsync(stoppingToken))
            {
                modelList.Add(reader.MapToUser());
            }

            _logger.LogInformation("Reading TMS_APIINFO successfully. API_CODE: {apiCode}, Total matching records: {count}", "System", modelList.Count);

            _logger.LogInformation("Reading User successfully. State: {state}, Total matching records: {count}", 1, modelList.Count);

            foreach (var model in modelList)
            {
                Console.WriteLine(model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Unhandled exception occurred during testing:\n{exception}", ex.ToString());
        }

        _logger.LogInformation("Finished Testing `ExecuteReaderAsync`");
    }

    public async Task ExecuteScalarAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Start to test `ExecuteScalarAsync`");

        try
        {
            string sql = "SELECT COUNT(*) FROM [User];";

            using var connection = SqlHelper.CreateConnection(ConnectionString, ProviderInvariantName);
            using var command = SqlHelper.CreateCommand(connection, null, CommandType.Text, sql);
            object count = await SqlHelper.ExecuteScalarAsync(command, stoppingToken);

            _logger.LogInformation("Reading User successfully. Total records: {count}", count);
        }
        catch (Exception ex)
        {
            _logger.LogError("Unhandled exception occurred during testing:\n{exception}", ex.ToString());
        }

        _logger.LogInformation("Finished Testing `ExecuteScalarAsync`");
    }

    public async Task ExecuteTransactionAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starts to test `ExecuteTransactionAsync`");

        try
        {
            DbParameter[] parameters1 = [
                SqlHelper.CreateParameter(ProviderInvariantName, "@p11", DbType.String, "usr001")
            ];
            string sql1 = "DELETE FROM [User] WHERE UserCode = @p11;";

            DbParameter[] parameters2 = [
                SqlHelper.CreateParameter(ProviderInvariantName, "@p21", DbType.String, "usr001"),
                SqlHelper.CreateParameter(ProviderInvariantName, "@p22", DbType.String, "Tom"),
                SqlHelper.CreateParameter(ProviderInvariantName, "@p23", DbType.String, "W3Rb3AoWjXs="),
                SqlHelper.CreateParameter(ProviderInvariantName, "@p24", DbType.String, "tom@gmail.com"),
                SqlHelper.CreateParameter(ProviderInvariantName, "@p25", DbType.String, "18618618666")
            ];
            string sql2 = $$"""
            INSERT INTO [User] (UserCode,UserName,Password,Email,Phone,Attributes,[State],CreatedTime,LastModifiedTime) VALUES
                (@p21,@p22,@p23,@p24,@p25,1,1,GETDATE(),GETDATE());
            """;

            using var connection = await SqlHelper.CreateConnectionAndOpenAsync(ConnectionString, ProviderInvariantName, stoppingToken);
            using var transaction = await connection.BeginTransactionAsync(stoppingToken);
            int affectedRows = 0;

            try
            {
                _logger.LogInformation("Start to execute sql1");
                using var command = SqlHelper.CreateCommand(null, transaction, CommandType.Text, sql1, parameters1);
                affectedRows = await SqlHelper.ExecuteNonQueryAsync(command, stoppingToken);
                _logger.LogInformation("Removing User successfully. User code: {userCode}, affected records: {count}", "usr001", affectedRows);

                _logger.LogInformation("Start to execute sql2");
                _ = SqlHelper.ReuseCommand(command, CommandType.Text, sql2, parameters2);
                affectedRows = await SqlHelper.ExecuteNonQueryAsync(command, stoppingToken);
                _logger.LogInformation("Adding User successfully. User code: {userCode}, affected records: {count}", "usr001", affectedRows);

                await transaction.CommitAsync(stoppingToken);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(stoppingToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Unhandled exception occurred during testing:\n{exception}", ex.ToString());
        }

        _logger.LogInformation("Finished Testing `ExecuteTransactionAsync`");
    }
}
