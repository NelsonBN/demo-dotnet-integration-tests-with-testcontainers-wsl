using Dapper;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MySql.Data.MySqlClient;
using System.Data;

namespace Demo.Tests;

public class IntegrationTestsConfig : WebApplicationFactory<ProductRequest>, IAsyncLifetime
{
    //private const string DB_IMAGE = "mysql:8.0.32";
    private const string DB_IMAGE = "mariadb:10.5.8";
    private static readonly int DB_HOST_PORT = Random.Shared.Next(50_000, 55_000);
    private const int DB_CONTAINER_PORT = 3306;
    private const string DB_DATABASE = "demo";
    private const string DB_USERNAME = "root";
    private const string DB_ROOT_PASSWORD = "testpassword";


    private readonly IContainer _dbContainer = new ContainerBuilder()
        .WithImage(DB_IMAGE)
        .WithName($"db-integration-tests-{Guid.NewGuid().ToString()[^5..]}")
#if RUN_LOCAL
        .WithDockerEndpoint("tcp://localhost:2375")
#endif
        .WithEnvironment("MYSQL_ROOT_PASSWORD", DB_ROOT_PASSWORD)
        .WithEnvironment("MYSQL_DATABASE", DB_DATABASE)
        .WithPortBinding(DB_HOST_PORT, DB_CONTAINER_PORT)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(DB_CONTAINER_PORT))
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll(typeof(IDbConnection));
            services.AddScoped<IDbConnection>(sp =>
            {
                var connection = new MySqlConnection($"server=localhost; Port={DB_HOST_PORT}; database={DB_DATABASE}; uid={DB_USERNAME}; password={DB_ROOT_PASSWORD};");
                connection.Open();

                return connection;
            });

            _prepareDatabase(services);
        });
    }

    private static void _prepareDatabase(IServiceCollection services)
    {
        using var providor = services.BuildServiceProvider();
        using var connection = providor.GetRequiredService<IDbConnection>();

        connection.Execute(File.ReadAllText("db-init.sql"));
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
    }
}