using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Claims.IntegrationTests;

[Collection(ApiCollection.Name)]
public class HealthTests(ClaimsApiFactory factory)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task With_both_databases_up_the_app_is_healthy(string url)
    {
        var response = await factory.CreateClient().GetAsync(url, CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(CancellationToken));
    }

    [Fact]
    public async Task Without_MongoDB_the_app_is_alive_but_not_ready()
    {
        await using var app = factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:Claims"] = "mongodb://localhost:1" })));
        var client = app.CreateClient();

        var live = await client.GetAsync("/health/live", CancellationToken);
        var ready = await client.GetAsync("/health/ready", CancellationToken);

        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, ready.StatusCode);
        Assert.Equal("Unhealthy", await ready.Content.ReadAsStringAsync(CancellationToken));
    }
}
