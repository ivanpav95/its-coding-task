using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Claims.IntegrationTests;

/// <summary>
/// The real app with its SQL Server and MongoDB containers, started once for all tests in <see cref="ApiCollection"/>.
/// The clock is fixed, so dates in the tests never depend on when they run.
/// </summary>
public sealed class ClaimsApiFactory : WebApplicationFactory<Program>
{
    public static readonly DateOnly Today = new(2030, 1, 10);

    public static readonly DateTime Now = new(2030, 1, 10, 12, 0, 0, DateTimeKind.Utc);

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureTestServices(services =>
            services.AddSingleton<TimeProvider>(new FakeTimeProvider(new DateTimeOffset(Now))));
}

[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<ClaimsApiFactory>
{
    public const string Name = "Claims API";
}
