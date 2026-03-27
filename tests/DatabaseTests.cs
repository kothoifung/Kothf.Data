using System.Data;
using System.Data.Common;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Kothf.Data.Tests;

/// <summary>
/// Test of <see cref="Database"/>
/// </summary>
internal sealed class DatabaseTests
{
    private readonly IDatabase _database;
    private readonly ILogger<DatabaseTests> _logger;

    public DatabaseTests(IDatabase database, ILogger<DatabaseTests> logger)
    {
        _database = database;
        _logger = logger;
    }

    public async Task ExecuteReaderAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starts to test `ExecuteReaderAsync`");

        try
        {
            List<User> modelList = [];
            int state = 1;

            DbParameter[] parameters = [
                _database.CreateParameter("@p1", DbType.Int32, state)
            ];

            string sql = $$"""
            SELECT
                UserCode,UserName,Password,Email,Phone,Attributes,[State],CreatedTime,LastModifiedTime
                FROM [User]
                WHERE [State] = @p1;
            """;

            using var reader = await _database.ExecuteReaderAsync(CommandType.Text, sql, parameters, stoppingToken);

            while (await reader.ReadAsync(stoppingToken))
            {
                modelList.Add(reader.MapToUser());
            }

            _logger.LogInformation("Reading User successfully. State: {state}, Total matching records: {count}", state, modelList.Count);

            foreach (var model in modelList)
            {
                Console.WriteLine(model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Exception occurred during testing:\n{exception}", ex.ToString());
        }

        _logger.LogInformation("Finished Testing `ExecuteReaderAsync`");
    }

    public async Task ExecuteScalarAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starts to test `ExecuteScalarAsync`");

        try
        {
            string sql = "SELECT COUNT(*) FROM [User];";

            object? count = await _database.ExecuteScalarAsync(CommandType.Text, sql, cancellationToken: stoppingToken);

            _logger.LogInformation("Reading User successfully. Total records: {count}", count);
        }
        catch (Exception ex)
        {
            _logger.LogError("Exception occurred during testing:\n{exception}", ex.ToString());
        }

        _logger.LogInformation("Finished Testing `ExecuteScalarAsync`");
    }

    public async Task ExecuteTransactionAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starts to test `ExecuteTransactionAsync`");

        try
        {
            string userCode = "usr001";
            DbParameter[] parameters1 = [
                _database.CreateParameter("@p11", DbType.String, userCode)
            ];
            string sql1 = "DELETE FROM [User] WHERE UserCode = @p11;";

            DbParameter[] parameters2 = [
                _database.CreateParameter("@p21", DbType.String, userCode),
                _database.CreateParameter("@p22", DbType.String, "James"),
                _database.CreateParameter("@p23", DbType.String, "W3Rb3AoWjXs="),
                _database.CreateParameter("@p24", DbType.String, "James.Bond@gmail.com"),
                _database.CreateParameter("@p25", DbType.String, "18618618666")
            ];
            string sql2 = $$"""
            INSERT INTO [User] (UserCode,UserName,Password,Email,Phone,Attributes,[State],CreatedTime,LastModifiedTime) VALUES
                (@p21,@p22,@p23,@p24,@p25,1,1,GETDATE(),GETDATE());
            """;

            try
            {
                int affectedRows;

                await _database.BeginTransactionAsync(cancellationToken: stoppingToken);
                _logger.LogInformation("Starts to execute sql1: Removing user");
                affectedRows = await _database.ExecuteNonQueryAsync(CommandType.Text, sql1, parameters1, stoppingToken);

                _logger.LogInformation("Starts to execute sql2: Adding user");
                affectedRows = await _database.ExecuteNonQueryAsync(CommandType.Text, sql2, parameters2, stoppingToken);

                await _database.CommitAsync(stoppingToken);
            }
            catch (Exception)
            {
                await _database.RollbackAsync(stoppingToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Exception occurred during testing:\n{exception}", ex.ToString());
        }

        _logger.LogInformation("Finished Testing `ExecuteTransactionAsync`");
    }
}
