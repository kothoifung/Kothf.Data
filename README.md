# Kothf.Data

A lightweight ADO.NET helper library that provides a `Database` object, and a `SqlHelper` utility.

## Usage

### Usage of `Database`

```csharp
public async Task ExecuteTransactionAsync(CancellationToken stoppingToken)
{
    _logger.LogInformation("Starts to test `ExecuteTransactionAsync`");

    try
    {
        DbParameter[] parameters1 = [
            _database.CreateParameter("@p11", DbType.String, "usr001")
        ];
        string sql1 = "DELETE FROM [User] WHERE UserCode = @p11;";

        DbParameter[] parameters2 = [
            _database.CreateParameter("@p21", DbType.String, "usr001"),
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

            _logger.LogInformation("Starts to execute sql2: Adding User");
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
```

### Usage of `SqlHelper`

```csharp
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
            SqlHelper.CreateParameter(ProviderInvariantName, "@p22", DbType.String, "James"),
            SqlHelper.CreateParameter(ProviderInvariantName, "@p23", DbType.String, "W3Rb3AoWjXs="),
            SqlHelper.CreateParameter(ProviderInvariantName, "@p24", DbType.String, "James.Bond@gmail.com"),
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
            _logger.LogInformation("Start to execute sql1: Removing user");
            using var command = SqlHelper.CreateCommand(null, transaction, CommandType.Text, sql1, parameters1);
            affectedRows = await SqlHelper.ExecuteNonQueryAsync(command, stoppingToken);

            _logger.LogInformation("Start to execute sql2: Adding user");
            _ = SqlHelper.ReuseCommand(command, CommandType.Text, sql2, parameters2);
            affectedRows = await SqlHelper.ExecuteNonQueryAsync(command, stoppingToken);

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
        _logger.LogError("Exception occurred during testing:\n{exception}", ex.ToString());
    }

    _logger.LogInformation("Finished Testing `ExecuteTransactionAsync`");
}
```
