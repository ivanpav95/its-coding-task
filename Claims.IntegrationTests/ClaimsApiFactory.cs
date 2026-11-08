using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Testcontainers.MongoDb;
using Testcontainers.MsSql;
using Xunit;

namespace Claims.IntegrationTests;

/// <summary>
/// The real app on throwaway SQL Server and MongoDB containers, started once for all tests in <see cref="ApiCollection"/>.
/// The clock is fixed, so dates in the tests never depend on when they run.
/// </summary>
public sealed class ClaimsApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public static readonly DateOnly Today = new(2030, 1, 10);

    public static readonly DateTimeOffset Now = new(2030, 1, 10, 12, 0, 0, TimeSpan.Zero);

    private readonly MsSqlContainer _sqlServer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    private readonly MongoDbContainer _mongoDb = new MongoDbBuilder("mongo:8").Build();

    public async ValueTask InitializeAsync() =>
        await Task.WhenAll(_sqlServer.StartAsync(), _mongoDb.StartAsync());

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var auditConnectionString = new SqlConnectionStringBuilder(_sqlServer.GetConnectionString())
        {
            InitialCatalog = "ClaimsAudit"
        }.ConnectionString;

        builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:Audit"] = auditConnectionString,
                ["ConnectionStrings:Claims"] = _mongoDb.GetConnectionString()
            }));

        var time = new FakeTimeProvider(Now);
        time.SetLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Europe/Oslo"));

        builder.ConfigureTestServices(services => services.AddSingleton<TimeProvider>(time));
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _sqlServer.DisposeAsync();
        await _mongoDb.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<ClaimsApiFactory>
{
    public const string Name = "Claims API";
}
