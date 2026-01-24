using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kothf.Data.Tests;

internal sealed class TestsWorker : BackgroundService
{
    private readonly ILogger<TestsWorker> _logger;
    private readonly IServiceProvider _serviceProvider;

    public TestsWorker(ILogger<TestsWorker> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Starts to test `Database`");

            var databaseTests = _serviceProvider.GetRequiredService<DatabaseTests>();

            await databaseTests.ExecuteScalarAsync(stoppingToken);
            await databaseTests.ExecuteReaderAsync(stoppingToken);
            await databaseTests.ExecuteTransactionAsync(stoppingToken);

            _logger.LogInformation("Tests `Database` complete");


///////////////////////////////////////////////////////////////////////////////


            _logger.LogInformation("Starts to test `SqlHelper`");

            var sqlHelperTests = _serviceProvider.GetRequiredService<SqlHelperTests>();

            await sqlHelperTests.ExecuteScalarAsync(stoppingToken);
            await sqlHelperTests.ExecuteReaderAsync(stoppingToken);
            await sqlHelperTests.ExecuteTransactionAsync(stoppingToken);

            _logger.LogInformation("Tests `SqlHelper` complete");

            await Task.Delay(1000, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Tests canceled, Ready to exit ...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred during testing!\n{exception}", ex.ToString());
            await Task.Delay(3000, stoppingToken);
        }
    }
}
