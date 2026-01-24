using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Kothf.Data.Tests;

internal class Program
{
    private const string DB_PROVIDER_MSSQL = "Microsoft.Data.SqlClient";

    static Program()
    {
        // 1. Register the SQL Server provider factory
        System.Data.Common.DbProviderFactories.RegisterFactory(DB_PROVIDER_MSSQL, Microsoft.Data.SqlClient.SqlClientFactory.Instance);
    }

    internal static async Task Main(string[] args)
    {
        try
        {
            var builder = Host.CreateEmptyApplicationBuilder(new() { Args = args });

            builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            builder.Logging.AddSimpleConsole(options =>
                {
                    options.SingleLine = true;
                    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";
                });

            // 2. Read connection string from configuration file
            string connectionString = builder.Configuration.GetConnectionString("demo") ?? throw new InvalidOperationException("Missing connection string 'demo' in configuration.");

            // 3. Register services
            builder.Services
                .AddSingleton<ConnectionStringSettings>(provider => new(connectionString, DB_PROVIDER_MSSQL))
                .AddTransient<IDatabase, Database>();

            builder.Services
                .AddTransient<DatabaseTests>()
                .AddTransient<SqlHelperTests>()
                .AddHostedService<TestsWorker>();

            var host = builder.Build();
            await host.RunAsync();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.ToString());
            Environment.ExitCode = 1;
        }
    }
}
