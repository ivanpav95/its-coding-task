using System.Net;
using Claims.Application.Contracts;
using Claims.Domain;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Claims.IntegrationTests;

[Collection(ApiCollection.Name)]
public class ClaimsEndpointTests(ClaimsApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_created_claim_is_listed()
    {
        var cover = await _client.CreateCoverAsync();
        var claim = await _client.CreateClaimAsync(cover.Id);

        var response = await _client.GetAsync("/Claims", CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(claim, await response.ReadAsync<List<ClaimResponse>>());
    }

    [Fact]
    public async Task A_claim_for_an_unknown_cover_is_a_validation_problem()
    {
        var response = await _client.PostClaimAsync(
            new CreateClaimRequest("unknown", ClaimsApiFactory.Now, "Hull damage", ClaimType.Collision, 5_000m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.ReadAsync<ValidationProblemDetails>();
        Assert.Contains(nameof(CreateClaimRequest.CoverId), problem.Errors.Keys);
    }

    [Fact]
    public async Task Damage_before_the_cover_was_bought_is_rejected_even_on_its_first_day()
    {
        var cover = await _client.CreateCoverAsync(); // starts today, so in force from Now
        var threeHoursEarlier = ClaimsApiFactory.Now.AddHours(-3);

        var response = await _client.PostClaimAsync(
            new CreateClaimRequest(cover.Id, threeHoursEarlier, "Hull damage", ClaimType.Collision, 5_000m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.ReadAsync<ValidationProblemDetails>();
        Assert.Contains(nameof(CreateClaimRequest.Created), problem.Errors.Keys);
    }

    [Fact]
    public async Task A_time_without_an_offset_is_rejected()
    {
        var cover = await _client.CreateCoverAsync();
        var json = $$"""{"coverId":"{{cover.Id}}","created":"2030-01-10T12:00:00","name":"Hull damage","type":"Collision","damageCost":5000}""";

        var response = await _client.PostAsync("/Claims",
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"), CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.ReadAsync<ValidationProblemDetails>();
        Assert.Contains(problem.Errors, error => error.Key.Contains("created", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task An_unknown_claim_is_not_found()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/Claims/unknown", CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.DeleteAsync("/Claims/unknown", CancellationToken)).StatusCode);
    }
}
