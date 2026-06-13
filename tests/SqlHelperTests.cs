using System.Data;
using Microsoft.Data.Sqlite;
using Xunit;
using Kothf.Data;

namespace Kothf.Data.Tests;

public sealed class SqlHelperTests : IAsyncLifetime
{
    private const string _sharedInMemory = "Data Source=SqlHelperTests;Mode=Memory;Cache=Shared";
    private SqliteConnection? _keeper;

    public async ValueTask InitializeAsync()
    {
        _keeper = new SqliteConnection(_sharedInMemory);
        await _keeper.OpenAsync(TestContext.Current.CancellationToken);

        await using var setup = _keeper.CreateCommand();
        setup.CommandText = """
            CREATE TABLE IF NOT EXISTS Users(Id INTEGER PRIMARY KEY, Name TEXT);
            DELETE FROM Users;
            INSERT INTO Users(Name) VALUES ('Alice'), ('Bob');
            """;
        await setup.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_keeper is not null)
        {
            await _keeper.DisposeAsync();
            _keeper = null;
        }
    }

    [Fact]
    public async Task ExecuteScalarAsync_OpensExecutesAndCloses()
    {
        await using var connection = new SqliteConnection(_sharedInMemory);
        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = "SELECT COUNT(*) FROM Users;";

        object? result = await SqlHelper.ExecuteScalarAsync(command, TestContext.Current.CancellationToken);

        Assert.Equal(2L, Assert.IsType<long>(result));
        Assert.Equal(ConnectionState.Closed, connection.State);
    }
}
